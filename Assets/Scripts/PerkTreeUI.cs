using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The perk tree on the picker slide. Follows the mask selector (each mask is a profession), lays the nodes
// out by itself from each node's Tier (height) and Branch (sideways), and lets the player buy nodes and
// switch which major is active per tier.
public class PerkTreeUI : MonoBehaviour
{
    [SerializeField] private ProfessionSet professions;
    [SerializeField] private MaskSelectUI masks;          // the selector on the same slide
    [SerializeField] private PerkNodeButton nodePrefab;
    [SerializeField] private RectTransform treeArea;      // an empty RectTransform: the tree grows up from its bottom centre

    [Header("Texts")]
    [SerializeField] private TMP_Text professionText;     // "SOLDIER  LV 3"
    [SerializeField] private TMP_Text pointsText;         // "2 POINTS"
    [SerializeField] private TMP_Text descriptionText;    // the hovered/clicked node's description 
    [SerializeField] private Image xpFill;                // Image Type = Filled, for ProfessionProgress.LevelProgress 

    [SerializeField] private TMP_Text leftBranchLabel;    // branch 1's name, from its first major ("FRONTLINE")
    [SerializeField] private TMP_Text rightBranchLabel;   // branch 2's name ("MARKSMAN")
    [SerializeField, Range(0f, 1f)] private float unusedBranchAlpha = 0.4f;   // the label of the branch not in use is dimmed

    [Header("Layout")]
    [SerializeField] private float tierSpacing = 110f;    // pixels between tiers (up)
    [SerializeField] private float branchSpacing = 180f;  // pixels between branches (sideways)

    [Header("Connection lines (optional)")]
    [SerializeField] private Image linePrefab;            // a plain white Image (any size: the code sizes it)
    [SerializeField] private float lineWidth = 6f;
    [SerializeField] private Vector2 nodeSize = new Vector2(140f, 90f);   // the node prefab's width x height (majors are drawn 1.2x)
    [SerializeField] private float lineGap = 4f;          // space between a line's end and the node's border
    [SerializeField] private Color lineLocked = new Color(0f, 0f, 0f, 0.25f);
    [SerializeField] private Color lineBought = new Color(1f, 1f, 1f, 0.6f);
    [SerializeField] private Color lineActive = new Color(1f, 0.8f, 0.35f, 1f);

    private ProfessionData profession;
    private readonly List<PerkNodeButton> buttons = new List<PerkNodeButton>();
    private readonly List<(Image line, PerkNode from, PerkNode to)> lines = new List<(Image, PerkNode, PerkNode)>();

    void OnEnable()
    {
        if (masks != null) masks.Changed += OnMaskChanged;
        if (masks != null) OnMaskChanged(masks.Index);    // show the current mask's tree straight away
    }

    void OnDisable() { if (masks != null) masks.Changed -= OnMaskChanged; }

    void OnMaskChanged(int maskIndex) => Show(professions.ForMask(maskIndex));

    // Branch 0 in the middle, odd branches to the left, even ones to the right (1 = one step left,
    // 2 = one step right, 3 = two steps left...). Tier 0 at the bottom, higher tiers above it.
    Vector2 NodePosition(PerkNode node)
    {
        int steps = (node.Branch + 1) / 2;
        float side = node.Branch == 0 ? 0f : (node.Branch % 2 == 1 ? -1f : 1f);
        return new Vector2(side * steps * branchSpacing, node.Tier * tierSpacing);
    }

    public void Show(ProfessionData newProfession)
    {
        profession = newProfession;
        RunProfession.SetProfession(profession);   // the picked mask's profession is the one this run plays

        // every profession's tree has a different shape, so rebuild it instead of reusing the buttons
        foreach (PerkNodeButton button in buttons)
        {
            Destroy(button.gameObject);
        }
        buttons.Clear();
        foreach (var l in lines) Destroy(l.line.gameObject);
        lines.Clear();

        if (profession == null)
        {
            professionText.text = "COMING SOON";
            pointsText.text = "";
            for (int branch = 1; branch <= 2; branch++) if (BranchLabel(branch) != null) BranchLabel(branch).text = "";
            return;
        }

        foreach (PerkNode node in profession.Nodes)
        {
            var button = Instantiate(nodePrefab, treeArea);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);   // bottom-centre: what NodePosition expects
            rect.anchoredPosition = NodePosition(node);
            button.Setup(node, OnNodeClicked);
            button.Hovered += ShowDescription;
            buttons.Add(button);

            // a line down to every node this one requires
            if (linePrefab == null) continue;
            foreach (PerkNode required in node.Requires)
                if (required != null) lines.Add((MakeLine(required, node), required, node));
        }

        // branch headers: each branch is named after its first major (the node where the player picks it)
        for (int branch = 1; branch <= 2; branch++)
        {
            if (BranchLabel(branch) == null) continue;
            PerkNode major = BranchMajor(branch);
            BranchLabel(branch).text = major != null ? major.Title.ToUpper() : "";
        }
        Refresh();
    }

    TMP_Text BranchLabel(int branch) => branch == 1 ? leftBranchLabel : branch == 2 ? rightBranchLabel : null;

    // The lowest major node on a branch, or null if the branch has none (the trunk)
    PerkNode BranchMajor(int branch)
    {
        PerkNode lowest = null;
        foreach (PerkNode node in profession.Nodes)
            if (node.IsMajor && node.Branch == branch && (lowest == null || node.Tier < lowest.Tier)) lowest = node;
        return lowest;
    }

    // A thin Image stretched and rotated between two nodes, starting and ending at their borders (not inside them)
    Image MakeLine(PerkNode from, PerkNode to)
    {
        Vector2 fromHalf = NodeHalfSize(from), toHalf = NodeHalfSize(to);
        Vector2 fromCentre = NodePosition(from) + Vector2.up * fromHalf.y;   // positions are the bottom-centre
        Vector2 toCentre = NodePosition(to) + Vector2.up * toHalf.y;
        Vector2 dir = (toCentre - fromCentre).normalized;

        // walk out from each centre along the line until it leaves the node's rectangle
        Vector2 a = fromCentre + dir * (DistanceToBorder(fromHalf, dir) + lineGap);
        Vector2 b = toCentre - dir * (DistanceToBorder(toHalf, dir) + lineGap);
        Vector2 direction = b - a;
        if (Vector2.Dot(direction, dir) <= 0f) direction = Vector2.zero;   // nodes overlap: no room for a line

        Image line = Instantiate(linePrefab, treeArea);
        line.raycastTarget = false;                    // lines must never block clicks on the nodes
        var rect = line.rectTransform;
        rect.SetAsFirstSibling();                      // drawn first = behind the nodes
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);            // grows upward from point a
        rect.anchoredPosition = a;
        rect.sizeDelta = new Vector2(lineWidth, direction.magnitude);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);   // -90: the Image points up, not right
        return line;
    }

    Vector2 NodeHalfSize(PerkNode node) => nodeSize * (node.IsMajor ? 1.2f : 1f) * 0.5f;   // majors are scaled 1.2 (PerkNodeButton)

    // How far from a rectangle's centre a ray in direction 'dir' hits its border:
    // whichever edge it reaches first, the side one (half.x) or the top/bottom one (half.y)
    static float DistanceToBorder(Vector2 half, Vector2 dir)
    {
        float toSide = Mathf.Abs(dir.x) > 0.0001f ? half.x / Mathf.Abs(dir.x) : float.MaxValue;
        float toTop = Mathf.Abs(dir.y) > 0.0001f ? half.y / Mathf.Abs(dir.y) : float.MaxValue;
        return Mathf.Min(toSide, toTop);
    }

    // Not bought: buy it (a freshly bought major is switched on if its tier has none yet).
    // Bought major: switch to it. Bought minors aren't clickable (PerkNodeButton.SetState).
    void OnNodeClicked(PerkNodeButton button)
    {
        PerkNode node = button.Node;
        switch (ProfessionProgress.IsBought(profession, node))
        {
            case false:
                if (ProfessionProgress.Buy(profession, node) && node.IsMajor)
                {
                    bool otherMajorChosen = false;
                    foreach (var n in profession.Nodes)
                    {
                        if (n.IsMajor && n.Tier == node.Tier && RunProfession.IsChosen(n))
                        {
                            otherMajorChosen = true;
                            break;
                        }
                    }
                    if (!otherMajorChosen)
                    {
                        RunProfession.Choose(profession, node);
                    }
                }
                break;
            case true:
                if (node.IsMajor)
                {
                    RunProfession.Choose(profession, node);
                }
                break;
        }
        Refresh();   // colours, active rings and the points text all change after a click
        ShowDescription(button);   // its status line changed too ("Costs 1 point" -> "ACTIVE")
    }

    // The description panel: the node's name, what it does, and what the player can do with it right now
    void ShowDescription(PerkNodeButton button)
    {
        if (descriptionText == null || profession == null) return;
        PerkNode node = button.Node;
        descriptionText.text = $"{node.Title}\n<size=70%>{node.Description}\n\n<alpha=#B3>{StatusLine(node)}";
    }

    string StatusLine(PerkNode node)
    {
        string points = node.Cost == 1 ? "1 point" : $"{node.Cost} points";
        if (RunProfession.IsActive(profession, node)) return "ACTIVE";
        if (ProfessionProgress.IsBought(profession, node))
            return node.IsMajor ? "LEARNED · click to use this branch" : "LEARNED · on the branch you're not using";
        foreach (PerkNode required in node.Requires)
            if (!ProfessionProgress.IsBought(profession, required)) return $"Requires {required.Title}";
        if (ProfessionProgress.CanBuy(profession, node)) return $"Costs {points} · click to learn";
        return $"Costs {points} · not enough points";
    }

    // Updates every button's look and the profession texts from the saved state

    void Refresh()
    {
        if (profession == null) return;
        foreach (PerkNodeButton button in buttons)
        {
            bool bought = ProfessionProgress.IsBought(profession, button.Node);
            bool active = RunProfession.IsActive(profession, button.Node);
            bool canBuy = ProfessionProgress.CanBuy(profession, button.Node);
            button.SetState(bought, active, canBuy);
        }
        // lines light up along the path that's active this run, dimmer where bought, faint where locked
        foreach (var (line, from, to) in lines)
        {
            if (RunProfession.IsActive(profession, to)) line.color = lineActive;
            else if (ProfessionProgress.IsBought(profession, to)) line.color = lineBought;
            else line.color = lineLocked;
        }
        // the branch in use keeps its header bright, the other one dims (all bright until a branch is picked)
        bool anyActive = false;
        for (int branch = 1; branch <= 2; branch++)
        {
            PerkNode major = BranchMajor(branch);
            if (major != null && RunProfession.IsActive(profession, major)) anyActive = true;
        }
        for (int branch = 1; branch <= 2; branch++)
        {
            if (BranchLabel(branch) == null) continue;
            PerkNode major = BranchMajor(branch);
            bool used = !anyActive || (major != null && RunProfession.IsActive(profession, major));
            BranchLabel(branch).alpha = used ? 1f : unusedBranchAlpha;
        }

        professionText.text = $"{profession.Title}  LV {ProfessionProgress.Level(profession)}";
        pointsText.text = $"{ProfessionProgress.PointsLeft(profession)} POINTS";
        if (xpFill != null)
        {
            xpFill.fillAmount = ProfessionProgress.LevelProgress(profession);
        }
    }

    // ---------- testing (Play mode: this component's ⋮ menu) ----------
    [ContextMenu("Debug: +100 XP to this profession")]
    void DebugAddXp()
    {
        if (profession == null) return;
        int levels = ProfessionProgress.AddXp(profession, 100);
        Debug.Log($"{profession.Title}: {ProfessionProgress.Xp(profession)} XP, level {ProfessionProgress.Level(profession)} (+{levels})");
        Refresh();
    }

    [ContextMenu("Debug: refund this profession's nodes")]
    void DebugRefund()
    {
        if (profession == null) return;
        ProfessionProgress.Refund(profession);
        Refresh();
    }
}
