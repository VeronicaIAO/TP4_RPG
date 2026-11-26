using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TPRPG
{
    public class BattleManager : MonoBehaviour
    {
        public enum State { Preparing, Running, Victory, Defeat }

        public BattleConfig config;
        public InventoryData startingInventory;

        public List<BattleUnit> players = new List<BattleUnit>();
        public List<BattleUnit> enemies = new List<BattleUnit>();
        public List<BattleUnit> reserve = new List<BattleUnit>();
        public Vector3 benchOrigin = new Vector3(0f, 0.05f, -60f);
        public float benchSpacing = 4f;

        public BattleUI ui;
        public FeedbackManager feedback;
        public EnemyAI ai;
        public ActionChoreography choreography;

        [Header("Change teammate")]
        [Min(0.1f)] public float swapDelay = 0.6f;
        [Range(0f, 100f)] public float swapInATB = 0f;

        [SerializeField] State state = State.Preparing;
        [SerializeField] bool executingAction;

        class PendingAction
        {
            public BattleUnit user;
            public ActionData action;
            public BattleUnit chosenTarget;
            public BattleUnit swapTo;
        }

        readonly Queue<PendingAction> queue = new Queue<PendingAction>();
        readonly HashSet<BattleUnit> queued = new HashSet<BattleUnit>();
        readonly List<BattleUnit> waitingPlayers = new List<BattleUnit>();

        public Inventory Inventory { get; private set; }
        public State CurrentState => state;
        public bool BattleOver => state == State.Victory || state == State.Defeat;
        public BattleUnit ActivePlayer => waitingPlayers.Count > 0 ? waitingPlayers[0] : null;
        public BattleUnit NextWaitingPlayer => waitingPlayers.Count > 1 ? waitingPlayers[1] : null;
        public int WaitingPlayers => waitingPlayers.Count;
        public bool CanSwap
        {
            get
            {
                if (state != State.Running || executingAction || queue.Count > 0 || ActivePlayer == null) return false;
                foreach (var r in reserve) if (r != null && r.IsAlive) return true;
                return false;
            }
        }
        public bool ATBPaused { get; private set; }

        float Speed => config != null ? config.animationSpeed : 1f;

        void Start()
        {
            if (config == null) config = ScriptableObject.CreateInstance<BattleConfig>();
            if (choreography == null) choreography = GetComponent<ActionChoreography>();
            if (choreography == null) choreography = gameObject.AddComponent<ActionChoreography>();
            if (choreography.feedback == null) choreography.feedback = feedback;
            Inventory = new Inventory(startingInventory);
            players.RemoveAll(u => u == null);
            enemies.RemoveAll(u => u == null);
            reserve.RemoveAll(u => u == null);
            foreach (var u in players) u.Initialize(this, true);
            foreach (var u in enemies) u.Initialize(this, false);
            for (int i = 0; i < reserve.Count; i++)
            {
                reserve[i].Initialize(this, true);
                reserve[i].transform.SetPositionAndRotation(benchOrigin + Vector3.right * benchSpacing * i, Quaternion.identity);
            }

            if (ui != null) ui.Initialize(this);
            Log("The fight begins!");
            state = State.Running;
        }

        public List<BattleUnit> TeamOf(BattleUnit u)
        {
            if (!u.isPlayerSide) return enemies;
            var all = new List<BattleUnit>(players);
            all.AddRange(reserve);
            return all;
        }
        public List<BattleUnit> RivalsOf(BattleUnit u) => u.isPlayerSide ? enemies : players;
        public IEnumerable<BattleUnit> All()
        {
            foreach (var u in players) yield return u;
            foreach (var u in enemies) yield return u;
        }

        void Update()
        {
            if (state != State.Running) return;

            bool actionPause = config.pauseATBDuringActions && executingAction;
            bool waitPause = config.atbMode == ATBMode.Wait && waitingPlayers.Count > 0;
            ATBPaused = actionPause || waitPause;

            if (!actionPause)
            {
                float dt = Time.deltaTime;
                foreach (var u in All())
                {
                    if (waitPause && !u.isPlayerSide) continue;
                    if (!u.IsAlive || queued.Contains(u) || waitingPlayers.Contains(u)) continue;
                    u.AdvanceATB(dt, config.globalATBSpeed);
                    if (u.ReadyToAct) UnitReady(u);
                }
            }

            if (!executingAction && queue.Count > 0) StartCoroutine(ProcessQueue());
        }

        void UnitReady(BattleUnit u)
        {
            if (u.isPlayerSide)
            {
                waitingPlayers.Add(u);
                if (waitingPlayers.Count == 1) ShowNextPlayer();
                if (feedback != null) feedback.Sound(FeedbackSound.Turn);
                if (u.Voice != null) u.Voice.PlayTurnReady();
            }
            else
            {
                var d = ai != null ? ai.Decide(u, enemies, players) : new EnemyAI.Decision { action = u.data.basicAttack };
                if (d.action == null) { u.EndTurn(0f); return; }
                Enqueue(u, d.action, d.target);
            }
        }

        void ShowNextPlayer()
        {
            if (ui == null) return;
            if (ActivePlayer != null) ui.ShowCommands(ActivePlayer);
            else ui.HideMenus();
        }

        public void CycleActivePlayer()
        {
            if (waitingPlayers.Count < 2) return;
            var first = waitingPlayers[0];
            waitingPlayers.RemoveAt(0);
            waitingPlayers.Add(first);
            ShowNextPlayer();
        }

        public void SubmitPlayerAction(BattleUnit u, ActionData action, BattleUnit target)
        {
            if (BattleOver || u == null || action == null || !waitingPlayers.Contains(u)) return;

            if (action is ItemData item && !Inventory.Consume(item))
            {
                Log($"No {item.displayName} left.");
                ShowNextPlayer();
                return;
            }
            if (action is SkillData s && u.currentMana < s.manaCost)
            {
                Log($"{u.DisplayName} doesn't have enough MP.");
                ShowNextPlayer();
                return;
            }

            waitingPlayers.Remove(u);
            Enqueue(u, action, target);
            ShowNextPlayer();
        }

        void Enqueue(BattleUnit u, ActionData a, BattleUnit target)
        {
            queue.Enqueue(new PendingAction { user = u, action = a, chosenTarget = target });
            queued.Add(u);
        }

        IEnumerator ProcessQueue()
        {
            executingAction = true;
            while (queue.Count > 0 && state == State.Running)
            {
                var p = queue.Dequeue();
                queued.Remove(p.user);
                if (p.swapTo != null)
                {
                    yield return ExecuteSwap(p.user, p.swapTo);
                    continue;
                }
                if (!p.user.IsAlive)
                {
                    if (p.action is ItemData it) Inventory.Refund(it);
                    continue;
                }
                yield return ExecuteAction(p);
                CheckBattleEnd();
                if (config.delayBetweenActions > 0f) yield return new WaitForSeconds(config.delayBetweenActions);
            }
            executingAction = false;
        }

        IEnumerator ExecuteAction(PendingAction p)
        {
            var u = p.user;
            var a = p.action;
            u.BeginTurn();

            if (a is SkillData s && s.manaCost > 0 && u.currentMana < s.manaCost)
            {
                Log($"{u.DisplayName} has no MP for {s.displayName}: uses {u.data.basicAttack.displayName}.");
                a = u.data.basicAttack;
            }

            var targets = ActionResolver.ResolveTargets(a, u, p.chosenTarget, TeamOf(u), RivalsOf(u));
            if (targets.Count == 0)
            {
                Log($"{u.DisplayName} finds no target for {a.displayName}.");
                if (a is ItemData it) Inventory.Refund(it);
                u.EndTurn(0f);
                yield break;
            }

            if (a is SkillData sk) u.SpendMana(sk.manaCost);

            Log($"{u.DisplayName} used {a.displayName}!");

            var aliveBefore = new Dictionary<BattleUnit, bool>();
            foreach (var x in All()) aliveBefore[x] = x.IsAlive;

            yield return choreography.Play(a, u, targets, () => ApplyEffects(a, u, targets), Speed);

            foreach (var x in All())
            {
                if (!aliveBefore.TryGetValue(x, out bool before)) continue;
                if (before && !x.IsAlive) UnitFell(x);
                else if (!before && x.IsAlive) x.Visual.SetKnockedOut(false);
            }

            bool waitingChanged = false;
            for (int i = waitingPlayers.Count - 1; i >= 0; i--)
            {
                if (waitingPlayers[i].atb < BattleUnit.MaxATB)
                {
                    waitingPlayers.RemoveAt(i);
                    waitingChanged = true;
                }
            }
            if (waitingChanged) ShowNextPlayer();

            u.EndTurn(a.recoveryCost);
        }

        void ApplyEffects(ActionData a, BattleUnit u, List<BattleUnit> targets)
        {
            var results = new List<EffectResult>();
            foreach (var t in targets)
            {
                results.Clear();
                foreach (var e in a.effects) ActionResolver.Apply(e, a, u, t, config, results);
                for (int i = 0; i < results.Count; i++)
                {
                    if (feedback != null) feedback.ShowResult(results[i], i * 0.25f);
                }
            }
        }

        void UnitFell(BattleUnit x)
        {
            x.Visual.SetKnockedOut(true);
            if (x.Voice != null) x.Voice.PlayKnockedOut();
            Log($"{x.DisplayName} is knocked out!");
            if (feedback != null) feedback.Sound(FeedbackSound.KO);

            queued.Remove(x);
            if (waitingPlayers.Remove(x)) ShowNextPlayer();
            if (ui != null) ui.UnitFell(x);
        }

        void CheckBattleEnd()
        {
            if (BattleOver) return;
            bool enemiesLeft = false, playersLeft = false;
            foreach (var e in enemies) if (e.IsAlive) enemiesLeft = true;
            foreach (var p in players) if (p.IsAlive) playersLeft = true;

            if (!enemiesLeft) { Finish(State.Victory); return; }
            if (playersLeft) return;

            BattleUnit replacement = null;
            foreach (var r in reserve)
                if (r.IsAlive) { replacement = r; break; }
            if (replacement == null) { Finish(State.Defeat); return; }

            BattleUnit fallen = players[0];
            foreach (var p in players) if (!p.IsAlive) { fallen = p; break; }
            Swap(replacement, fallen, false);
            Log($"{replacement.DisplayName} steps in!");
            if (feedback != null) feedback.Sound(FeedbackSound.Turn);
            replacement.Visual.Flash(Color.white, 0.4f);
            if (ui != null) ui.RefreshTeam();
        }

        public bool SwapActivePlayer(BattleUnit incoming)
        {
            var outgoing = ActivePlayer;
            if (!CanSwap || incoming == null || !incoming.IsAlive || !reserve.Contains(incoming)) return false;

            waitingPlayers.Remove(outgoing);
            queue.Enqueue(new PendingAction { user = outgoing, swapTo = incoming });
            queued.Add(outgoing);
            ShowNextPlayer();
            return true;
        }

        IEnumerator ExecuteSwap(BattleUnit outgoing, BattleUnit incoming)
        {
            if (!outgoing.IsAlive || !incoming.IsAlive || !players.Contains(outgoing) || !reserve.Contains(incoming))
            {
                if (outgoing.IsAlive && players.Contains(outgoing))
                {
                    waitingPlayers.Insert(0, outgoing);
                    ShowNextPlayer();
                }
                yield break;
            }

            float s = Mathf.Max(0.1f, Speed);
            Log($"{outgoing.DisplayName}, come back!");
            if (feedback != null) feedback.Sound(FeedbackSound.Whoosh);
            outgoing.Visual.Flash(Color.white, swapDelay / s);
            yield return new WaitForSeconds(swapDelay / s);

            Swap(incoming, outgoing, false);
            outgoing.atb = 0f;
            incoming.atb = swapInATB;

            Log($"Go, {incoming.DisplayName}!");
            if (feedback != null) feedback.Sound(FeedbackSound.Turn);
            incoming.Visual.Flash(Color.white, 0.4f);
            if (ui != null) ui.RefreshTeam();
            yield return new WaitForSeconds(swapDelay * 0.5f / s);
        }

        void Swap(BattleUnit incoming, BattleUnit outgoing, bool exchangeAtb)
        {
            int pi = players.IndexOf(outgoing);
            int ri = reserve.IndexOf(incoming);
            if (pi < 0 || ri < 0) return;

            players[pi] = incoming;
            reserve[ri] = outgoing;

            int wi = waitingPlayers.IndexOf(outgoing);
            if (wi >= 0) waitingPlayers[wi] = incoming;
            queued.Remove(outgoing);

            if (exchangeAtb)
            {
                float t = incoming.atb;
                incoming.atb = outgoing.atb;
                outgoing.atb = t;
            }
            outgoing.guarding = false;
            outgoing.guardMultiplier = 1f;

            Vector3 pos = outgoing.transform.position;
            Quaternion rot = outgoing.transform.rotation;
            outgoing.transform.SetPositionAndRotation(incoming.transform.position, incoming.transform.rotation);
            incoming.transform.SetPositionAndRotation(pos, rot);
        }

        void Finish(State final)
        {
            state = final;
            queue.Clear();
            queued.Clear();
            waitingPlayers.Clear();

            bool won = final == State.Victory;
            var winners = won ? players : enemies;
            BattleUnit voiceWinner = null;
            foreach (var w in winners)
                if (w.IsAlive && voiceWinner == null) voiceWinner = w;
            if (voiceWinner != null && voiceWinner.Voice != null) voiceWinner.Voice.PlayVictory();

            string winnerName = voiceWinner != null ? voiceWinner.DisplayName : (won ? "You" : "The rival");
            Log(won ? "VICTORY!" : "Defeat...");
            if (feedback != null) feedback.Sound(won ? FeedbackSound.Victory : FeedbackSound.Defeat);
            if (ui != null) ui.ShowResult(won, winnerName);
        }

        public void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void Log(string msg)
        {
            if (ui != null) ui.AddLog(msg);
            else Debug.Log("[Battle] " + msg);
        }
    }
}