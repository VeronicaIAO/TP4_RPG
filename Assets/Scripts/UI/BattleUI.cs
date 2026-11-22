using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TPRPG
{
    public class BattleUI : MonoBehaviour
    {
        enum Menu { Hidden, Commands, Skills, Bag, Targets }

        public UnitStatusRow[] playerBoxes = new UnitStatusRow[0];
        public UnitStatusRow[] enemyBoxes = new UnitStatusRow[0];

        public Text dialogText;
        public GameObject continueIndicator;
        [Min(1)] public int dialogLines = 2;
        public Text atbModeText;

        public GameObject commandPanel;
        public Button attackButton;
        public Text attackLabel;
        public Button skillsButton;
        public Button itemsButton;
        public Button guardButton;

        public GameObject skillsPanel;
        public ListEntryUI[] skillButtons = new ListEntryUI[4];
        public Text skillCostText;
        public Button skillsBackButton;

        public GameObject bagPanel;
        public RectTransform bagContainer;
        public ListEntryUI entryTemplate;
        public Button bagCloseButton;

        public GameObject targetPanel;
        public RectTransform targetContainer;
        public Button targetCloseButton;

        public GameObject resultPanel;
        public Text resultText;
        public Text resultSubtitle;
        public Button restartButton;

        public bool autoSelectSingleTarget = true;

        BattleManager manager;
        readonly List<UnitStatusRow> boxes = new List<UnitStatusRow>();
        readonly List<GameObject> bagEntries = new List<GameObject>();
        readonly List<GameObject> targetEntries = new List<GameObject>();
        readonly List<BattleUnit> currentCandidates = new List<BattleUnit>();
        readonly Queue<string> log = new Queue<string>();

        Menu menu = Menu.Hidden;
        Menu returnMenu = Menu.Commands;
        BattleUnit currentUnit;
        ActionData pendingAction;

        public void Initialize(BattleManager m)
        {
            manager = m;

            for (int i = 0; i < playerBoxes.Length; i++) Bind(playerBoxes[i], i < m.players.Count ? m.players[i] : null);
            for (int i = 0; i < enemyBoxes.Length; i++) Bind(enemyBoxes[i], i < m.enemies.Count ? m.enemies[i] : null);
            if (entryTemplate != null) entryTemplate.gameObject.SetActive(false);

            attackButton.onClick.AddListener(() => { Click(); ChooseAction(currentUnit.data.basicAttack, Menu.Commands); });
            guardButton.onClick.AddListener(() => { Click(); ChooseAction(currentUnit.data.guardAction, Menu.Commands); });
            skillsButton.onClick.AddListener(() => { Click(); OpenSkills(); });
            itemsButton.onClick.AddListener(() => { Click(); OpenBag(); });
            skillsBackButton.onClick.AddListener(() => { Click(); BackToCommands(); });
            bagCloseButton.onClick.AddListener(() => { Click(); BackToCommands(); });
            targetCloseButton.onClick.AddListener(() =>
            {
                Click();
                if (returnMenu == Menu.Skills) OpenSkills();
                else if (returnMenu == Menu.Bag) OpenBag();
                else BackToCommands();
            });
            if (restartButton != null) restartButton.onClick.AddListener(() => manager.Restart());

            if (resultPanel != null) resultPanel.SetActive(false);
            if (continueIndicator != null) continueIndicator.SetActive(false);
            HideMenus();
        }

        void Bind(UnitStatusRow box, BattleUnit u)
        {
            if (box == null) return;
            box.gameObject.SetActive(u != null);
            if (u == null) return;
            box.Bind(u);
            boxes.Add(box);
        }

        void Click() { if (manager.feedback != null) manager.feedback.Sound(FeedbackSound.Select); }

        void Update()
        {
            if (manager == null) return;
            foreach (var b in boxes) b.Refresh(b.Unit == currentUnit && menu != Menu.Hidden);
            if (atbModeText != null)
                atbModeText.text = manager.config != null && manager.config.atbMode == ATBMode.Wait
                    ? "WAIT mode: time stops while you choose" : "ACTIVE mode: time keeps running";
            if (continueIndicator != null) continueIndicator.SetActive(menu == Menu.Hidden && !manager.BattleOver && Mathf.Repeat(Time.time, 0.8f) > 0.4f);

            if (menu == Menu.Targets && pendingAction != null && currentUnit != null)
            {
                var fresh = ActionResolver.Candidates(pendingAction.target, currentUnit, manager.TeamOf(currentUnit), manager.RivalsOf(currentUnit));
                if (!SameList(fresh, currentCandidates)) OpenTargets(pendingAction);
            }
        }

        static bool SameList(List<BattleUnit> a, List<BattleUnit> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++) if (a[i] != b[i]) return false;
            return true;
        }

        void ShowPanel(Menu m)
        {
            menu = m;
            commandPanel.SetActive(m == Menu.Commands);
            skillsPanel.SetActive(m == Menu.Skills);
            bagPanel.SetActive(m == Menu.Bag);
            targetPanel.SetActive(m == Menu.Targets);
            if (m != Menu.Targets) ClearTargetMarkers();
            if (m == Menu.Hidden) RefreshDialog();
        }

        void Focus(Selectable first)
        {
            if (EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(first != null && first.gameObject.activeInHierarchy ? first.gameObject : null);
        }

        void Prompt(string text)
        {
            if (dialogText != null) dialogText.text = text;
        }

        void RefreshDialog()
        {
            if (dialogText != null) dialogText.text = string.Join("\n", log.ToArray());
        }

        public void ShowCommands(BattleUnit u)
        {
            if (manager.BattleOver) return;
            if (u == currentUnit && menu != Menu.Hidden) return;
            if (currentUnit != null && currentUnit != u) currentUnit.Visual.SetTurnMarker(false);
            currentUnit = u;
            pendingAction = null;
            u.Visual.SetTurnMarker(true);
            BackToCommands();
        }

        void BackToCommands()
        {
            if (currentUnit == null) { HideMenus(); return; }
            var u = currentUnit;
            Prompt($"What will\n{u.DisplayName.ToUpper()} do?");
            attackButton.interactable = u.data.basicAttack != null;
            guardButton.interactable = u.data.guardAction != null;
            skillsButton.interactable = u.data.skills.Count > 0;
            itemsButton.interactable = manager.Inventory.HasAny();
            if (attackLabel != null && u.data.basicAttack != null) attackLabel.text = u.data.basicAttack.displayName.ToUpper();
            ShowPanel(Menu.Commands);
            Focus(attackButton);
        }

        public void HideMenus()
        {
            if (currentUnit != null) currentUnit.Visual.SetTurnMarker(false);
            currentUnit = null;
            pendingAction = null;
            ShowPanel(Menu.Hidden);
        }

        void OpenSkills()
        {
            var u = currentUnit;
            Selectable first = null;
            for (int i = 0; i < skillButtons.Length; i++)
            {
                var b = skillButtons[i];
                if (b == null) continue;
                bool exists = i < u.data.skills.Count && u.data.skills[i] != null;
                b.gameObject.SetActive(exists);
                if (!exists) continue;
                var s = u.data.skills[i];
                bool enoughMana = u.currentMana >= s.manaCost;
                var candidates = ActionResolver.Candidates(s.target, u, manager.TeamOf(u), manager.RivalsOf(u));
                b.Setup(s.displayName.ToUpper(), "", enoughMana && candidates.Count > 0);
                var img = b.button.targetGraphic as Image;
                if (img != null) img.color = s.effectColor;
                b.onHighlight = () => ShowSkillInfo(s);
                b.onClick = () => { Click(); ChooseAction(s, Menu.Skills); };
                if (first == null && b.button.interactable) first = b.button;
            }
            Prompt("");
            ShowPanel(Menu.Skills);
            if (u.data.skills.Count > 0 && u.data.skills[0] != null) ShowSkillInfo(u.data.skills[0]);
            Focus(first != null ? first : skillsBackButton);
        }

        void ShowSkillInfo(SkillData s)
        {
            if (skillCostText != null)
                skillCostText.text = $"MP   {s.manaCost}/{currentUnit.currentMana}";
        }

        void OpenBag()
        {
            foreach (var g in bagEntries) Destroy(g);
            bagEntries.Clear();
            Selectable first = null;
            foreach (var e in manager.Inventory.entries)
            {
                if (e.amount <= 0) continue;
                var item = e.item;
                var candidates = ActionResolver.Candidates(item.target, currentUnit, manager.TeamOf(currentUnit), manager.RivalsOf(currentUnit));
                var entry = Instantiate(entryTemplate, bagContainer);
                entry.gameObject.SetActive(true);
                entry.Setup(item.displayName, "x" + e.amount, candidates.Count > 0);
                entry.onClick = () => { Click(); ChooseAction(item, Menu.Bag); };
                bagEntries.Add(entry.gameObject);
                if (first == null && entry.button.interactable) first = entry.button;
            }
            Prompt("");
            ShowPanel(Menu.Bag);
            Focus(first != null ? first : bagCloseButton);
        }

        void ChooseAction(ActionData a, Menu from)
        {
            if (a == null || currentUnit == null) return;
            pendingAction = a;
            returnMenu = from;
            if (!ActionResolver.RequiresSelection(a.target)) { Confirm(null); return; }
            var candidates = ActionResolver.Candidates(a.target, currentUnit, manager.TeamOf(currentUnit), manager.RivalsOf(currentUnit));
            if (autoSelectSingleTarget && candidates.Count == 1) { Confirm(candidates[0]); return; }
            OpenTargets(a);
        }

        void OpenTargets(ActionData a)
        {
            foreach (var g in targetEntries) Destroy(g);
            targetEntries.Clear();
            ClearTargetMarkers();

            currentCandidates.Clear();
            currentCandidates.AddRange(ActionResolver.Candidates(a.target, currentUnit, manager.TeamOf(currentUnit), manager.RivalsOf(currentUnit)));

            Selectable first = null;
            foreach (var c in currentCandidates)
            {
                var unit = c;
                var e = Instantiate(entryTemplate, targetContainer);
                e.gameObject.SetActive(true);
                e.Setup(unit.DisplayName, unit.IsAlive ? $"{unit.currentHealth}/{unit.MaxHealth}" : "K.O.", true);
                e.onHighlight = () => { ClearTargetMarkers(); unit.Visual.SetTargetMarker(true); };
                e.onUnhighlight = () => unit.Visual.SetTargetMarker(false);
                e.onClick = () => { Click(); Confirm(unit); };
                targetEntries.Add(e.gameObject);
                if (first == null) first = e.button;
            }
            Prompt($"{a.displayName}: choose a target.");
            ShowPanel(Menu.Targets);
            Focus(first != null ? first : targetCloseButton);
        }

        void ClearTargetMarkers()
        {
            if (manager == null) return;
            foreach (var u in manager.All()) u.Visual.SetTargetMarker(false);
        }

        void Confirm(BattleUnit target)
        {
            var u = currentUnit;
            var a = pendingAction;
            ClearTargetMarkers();
            if (u != null) u.Visual.SetTurnMarker(false);
            currentUnit = null;
            pendingAction = null;
            ShowPanel(Menu.Hidden);
            manager.SubmitPlayerAction(u, a, target);
        }

        public void UnitClicked(BattleUnit u)
        {
            if (menu == Menu.Targets && currentCandidates.Contains(u)) { Click(); Confirm(u); }
        }

        public void UnitHovered(BattleUnit u, bool inside)
        {
            if (menu != Menu.Targets || !currentCandidates.Contains(u)) return;
            if (inside) ClearTargetMarkers();
            u.Visual.SetTargetMarker(inside);
        }

        public void UnitFell(BattleUnit u)
        {
            if (u == currentUnit) HideMenus();
        }

        public void AddLog(string msg)
        {
            log.Enqueue(msg);
            while (log.Count > dialogLines) log.Dequeue();
            if (menu == Menu.Hidden) RefreshDialog();
        }

        public void ShowResult(bool victory, string winnerName)
        {
            HideMenus();
            if (resultPanel == null) return;
            resultPanel.SetActive(true);
            resultText.text = victory ? "VICTORY!" : "DEFEAT";
            resultText.color = victory ? new Color(1f, 0.85f, 0.25f) : new Color(0.9f, 0.25f, 0.25f);
            if (resultSubtitle != null) resultSubtitle.text = $"{winnerName.ToUpper()} wins the fight!";
            Focus(restartButton);
        }
    }
}
