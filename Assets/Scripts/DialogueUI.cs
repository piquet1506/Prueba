using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; }
    public GameObject panel;
    public Text dialogueText;
    public Text prompt;
    public NpcDialogue Current { get; private set; }
    private Transform player;
    private NpcDialogue[] npcs;
    private int index;
    private int closedFrame = -1;
    public static int LastClosedFrame { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { IsOpen = false; LastClosedFrame = -1; }
    private void Start()
    {
        var obj = GameObject.FindGameObjectWithTag("Player");
        if (obj != null) player = obj.transform;
        npcs = FindObjectsByType<NpcDialogue>(FindObjectsSortMode.None);
        panel.SetActive(false); prompt.text = "";
    }
    private void Update()
    {
        if (player == null) return;
        if (IsOpen)
        {
            if (Current == null || !Current.InRange(player) || player.GetComponent<Health>().IsDead) { Close(); return; }
            if (Input.GetKeyDown(KeyCode.Escape)) Close();
            else if (Input.GetKeyDown(KeyCode.E)) Advance();
            return;
        }
        prompt.text = "";
        if (Time.timeScale == 0 || Time.frameCount == closedFrame || player.GetComponent<Health>().IsDead) return;
        NpcDialogue nearest = null;
        float distance = float.MaxValue;
        foreach (var npc in npcs)
        {
            if (npc == null || !npc.InRange(player)) continue;
            float d = Vector2.Distance(player.position, npc.transform.position);
            if (d < distance) { nearest = npc; distance = d; }
        }
        if (nearest == null) return;
        prompt.text = "E - Hablar con " + nearest.displayName;
        if (Input.GetKeyDown(KeyCode.E)) Begin(nearest);
    }
    public void Begin(NpcDialogue npc)
    {
        if (npc == null || !npc.InRange(player) || Time.timeScale == 0 || npc.lines == null || npc.lines.Length == 0) return;
        Current = npc; index = 0; IsOpen = true; prompt.text = ""; panel.SetActive(true);
        dialogueText.text = npc.lines[index];
    }
    public void Advance()
    {
        if (!IsOpen || Current == null) return;
        if (++index >= Current.lines.Length) Close();
        else dialogueText.text = Current.lines[index];
    }
    public void Close()
    {
        IsOpen = false; Current = null; panel.SetActive(false); prompt.text = "";
        LastClosedFrame = closedFrame = Time.frameCount;
    }
    private void OnDestroy() { IsOpen = false; }
}
