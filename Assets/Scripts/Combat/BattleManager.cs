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

        public BattleUI ui;
        public FeedbackManager feedback;
        public EnemyAI ai;
        public ActionChoreography choreography;

        [SerializeField] State state = State.Preparing;
        [SerializeField] bool executingAction;

        class PendingAction
        {
            public BattleUnit user;
            public ActionData action;
            public BattleUnit chosenTarget;
        }

        readonly Queue<PendingAction> queue = new Queue<PendingAction>();
        readonly HashSet<BattleUnit> queued = new HashSet<BattleUnit>();
        readonly List<BattleUnit> waitingPlayers = new List<BattleUnit>();

        public Inventory Inventory { get; private set; }
        public State CurrentState => state;
        public bool BattleOver => state == State.Victory || state == State.Defeat;
        public BattleUnit ActivePlayer => waitingPlayers.Count > 0 ? waitingPlayers[0] : null;
        public int WaitingPlayers => waitingPlayers.Count;
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
            foreach (var u in players) u.Initialize(this, true);
            foreach (var u in enemies) u.Initialize(this, false);

            if (ui != null) ui.Initialize(this);
            Log("The fight begins!");
            state = State.Running;
        }

        public List<BattleUnit> TeamOf(BattleUnit u) => u.isPlayerSide ? players : enemies;
        public List<BattleUnit> RivalsOf(BattleUnit u) => u.isPlayerSide ? enemies : players;
        public IEnumerable<BattleUnit> All()
        {
            foreach (var u in players) yield return u;
            foreach (var u in enemies) yield return u;
        }

        void Update()
        {
            if (state != State.Running) return;

            bool paused = (config.pauseATBDuringActions && executingAction) ||
                          (config.atbMode == ATBMode.Wait && waitingPlayers.Count > 0);
            ATBPaused = paused;

            if (!paused)
            {
                float dt = Time.deltaTime;
                foreach (var u in All())
                {
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

            u.Visual.SetTurnMarker(true);
            Log($"{u.DisplayName} used {a.displayName}!");

            var aliveBefore = new Dictionary<BattleUnit, bool>();
            foreach (var x in All()) aliveBefore[x] = x.IsAlive;

            yield return choreography.Play(a, u, targets, () => ApplyEffects(a, u, targets), Speed);

            foreach (var x in All())
            {
                bool before = aliveBefore[x];
                if (before && !x.IsAlive) UnitFell(x);
                else if (!before && x.IsAlive) x.Visual.Revive();
            }

            bool waitingChanged = false;
            for (int i = waitingPlayers.Count - 1; i >= 0; i--)
            {
                if (waitingPlayers[i].atb < BattleUnit.MaxATB)
                {
                    waitingPlayers[i].Visual.SetTurnMarker(false);
                    waitingPlayers.RemoveAt(i);
                    waitingChanged = true;
                }
            }
            if (waitingChanged) ShowNextPlayer();

            u.Visual.SetTurnMarker(false);
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
                    if (feedback != null) feedback.ShowResult(results[i], a, i * 0.25f);
                }
            }
            if (a.shakeCamera && feedback != null) feedback.ShakeCamera(0.2f, 0.3f);
        }

        void UnitFell(BattleUnit x)
        {
            x.Visual.Die();
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

            if (!enemiesLeft) Finish(State.Victory);
            else if (!playersLeft) Finish(State.Defeat);
        }

        void Finish(State final)
        {
            state = final;
            queue.Clear();
            queued.Clear();
            waitingPlayers.Clear();
            foreach (var u in All()) u.Visual.SetTurnMarker(false);

            bool won = final == State.Victory;
            var winners = won ? players : enemies;
            BattleUnit voiceWinner = null;
            foreach (var w in winners)
            {
                if (!w.IsAlive) continue;
                w.Visual.StartCelebration();
                if (voiceWinner == null) voiceWinner = w;
            }
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
