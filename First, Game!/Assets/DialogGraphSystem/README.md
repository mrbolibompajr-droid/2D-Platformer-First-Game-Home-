Dialog Graph System (v1.3.1)

Lightweight, production-friendly dialogue for Unity.
Author conversations with a Graph Editor (entry, choices, auto-next), and ship a polished UGUI + TextMeshPro runtime with typewriter, skip, autoplay and a history overlay.
New in 1.3.x: Action Nodes with an Action Runner (UnityEvents + async handlers), and a clean node model (Start / Dialog / Choice / Action / End).

Requirements

Unity 2021.3 LTS or newer

Packages: TextMesh Pro, UGUI

Editor graph uses UnityEditor.Experimental.GraphView (Editor-only)

Install

Import the .unitypackage into a clean project (LTS recommended).

Open the Demo scenes (see below) to try features right away.

Folder Structure (high-level)
Assets/
  DialogGraphSystem/
    Scenes/
      DialogDemo.unity                // dialog-only sample
      ActionDialogDemo.unity          // actions sample
    Scripts/
      Runtime/
        Core/                         // DialogManager, DialogActionRunner, MonoSingleton
        UI/                           // DialogUIController (UGUI bridge)
        Models/                       // DialogGraph + Nodes (Start/Dialog/Choice/Action/End)
        DialogHistory/                // history overlay scripts
        Interfaces/                   // IActionHandler
        Actions/                      // DemoHandler_Countdown, DemoUnityEventActions
        Utils/                        // PayloadHelper, TextResources
      Editor/
        Windows/                      // DialogGraphEditorWindow, DialogJsonIOWindow, SaveGraphPromptWindow
        View/                         // NodeViews per node type, DialogEdge
    Prefabs/                          // DialogUI_Panel, Choice_Btn, history prefabs
    Resources/
      Conversation/                   // sample ScriptableObject graphs
      UI/, USS/, Audio/               // demo assets

Demo Scenes (2)

DialogDemo.unity — linear + branching basics (no actions)

ActionDialogDemo.unity — shows Action Nodes, blocking handlers, UnityEvent bindings

Keep both scenes in the package (as you requested).

Quickstart (Dialog-only)

Create a graph: Tools → Dialog Graph Editor → New Graph → add nodes.

Use Start → Dialog → Choice / Action / End.

Use Auto-Next output on Dialog nodes for linear flows.

Drop DialogManager and DialogUI_Panel into your scene.

In DialogManager, add your graph to the Dialog Graphs List and assign a Dialog ID (string).

Assign DialogUIController reference (panel, name text, dialog text, portrait, choices container, choice button prefab, skip button, autoplay icons).

Start a conversation from code:

DialogSystem.Runtime.Core.DialogManager.Instance
    .PlayDialogByID("YourDialogID", onDialogEnded: () => Debug.Log("Done"));


Runtime UI basics:

Click panel to advance (or reveal then advance, depending on your chosen behavior).

Skip line / Skip all toggles are available via inspector flags.

Autoplay can be toggled from the panel button (play/pause icon syncs).

History / Backlog

PauseForHistory() pauses typing and auto-advance and shows the overlay.

ResumeAfterHistory() closes history and returns control to the player.

Entries are stored as Line or Choice and displayed with distinct styling.

Actions: Action Nodes + Runner + Handlers (core of 1.3.x)

Action nodes let you trigger gameplay or UI logic mid-conversation. They can be fire-and-forget (UnityEvent) or blocking (wait until a coroutine completes).

1) Concept

ActionNode (in the graph) has:

actionId (string) — the key that identifies the action

payloadJson (string) — parameters for the action (JSON)

waitForCompletion (bool) — if true, dialogue waits for the action to finish

waitSeconds (float) — optional pre-delay before invoking

DialogActionRunner (scene component):

Holds Global and Per-Conversation Action Sets

Each set can have:

UnityEvent bindings (ActionBinding) for synchronous actions

A list of handlers (MonoBehaviour components) that implement IActionHandler for asynchronous/blocking actions

DialogManager:

Traverses from a node to the next node, executing Action nodes in-between

If actionRunner is assigned, it routes actions to that runner and yields when needed

If no runner is assigned, Action nodes are skipped (no side-effects)

2) Setting up the Action Runner

Add DialogActionRunner to your scene.

In DialogManager, assign this runner into “Optional Actions Runner”.

Configure Global set:

Add ActionBindings (one per actionId) with UnityEvent<string> receiving the payloadJson.

Add handlers (MonoBehaviours) that implement IActionHandler for actions that should block.

(Optional) Configure Conversation sets:

Each set has a Conversation Key (string) — should match the Dialog ID you pass to PlayDialogByID(...).

Add bindings/handlers specific to that conversation.

If useGlobalFallback is enabled, the runner checks the conversation set first, then Global.

Example (inspector):

Global

Bindings:

actionId: "ui.flash" → On Invoke (string payload) → hook a UI Flash method

Handlers:

DemoHandler_Countdown (blocks until countdown completes)

Conversations

Key: "CoffeeAI"

Bindings:

actionId: "barista.sound" → play sfx with payload

Handlers:

Custom SteamValveHandler for "steam.release"

3) Authoring an Action Node (in the Graph)

Create an Action node, set:

Action Id (e.g., demo.countdown.sync)

Payload Json (e.g., { "seconds": 3 })

Wait for Completion = true to block the flow until done

Wait Seconds (optional pre-delay; e.g., 0.5)

Wire it between your nodes (e.g., Dialog → Action → Choice).
The manager will run the action, then continue to the next non-action node.

4) Writing an IActionHandler (async/await-style via coroutines)
using System.Collections;
using UnityEngine;
using DialogSystem.Runtime.Interfaces;

public class MyFadeHandler : MonoBehaviour, IActionHandler
{
    public bool CanHandle(string actionId) => actionId == "screen.fade";

    public IEnumerator Handle(string actionId, string payloadJson)
    {
        // parse payload (e.g., { "seconds": 1.25 })
        float seconds = 1.0f;
        // ...parse JSON yourself or use PayloadHelper (see below)...
        yield return StartCoroutine(FadeRoutine(seconds));
    }

    private IEnumerator FadeRoutine(float t)
    {
        // ... do your fade over t seconds ...
        yield return new WaitForSeconds(t);
    }
}


Add this component to DialogActionRunner → [Global/Conversation].handlers.

In your Action node, set actionId = "screen.fade" and waitForCompletion = true.

5) Using UnityEvents (fire-and-forget)

If your action is instant (no blocking needed), use ActionBindings:

Add an ActionBinding with actionId = "ui.shuffle".

Hook the UnityEvent<string> to your method: void OnActionInvoke(string payloadJson) { ... }.

In your Action node:

waitForCompletion = false

payloadJson is forwarded as argument

6) Optional: Call actions from code (no graph)

You can invoke the runner from code:

var dm = DialogSystem.Runtime.Core.DialogManager.Instance;

// Global action
StartCoroutine(dm.actionRunner.RunActionGlobal(
    actionId: "ui.flash",
    payload: "{\"color\":\"#FF8800\"}",
    waitForCompletion: false,
    waitSeconds: 0f));

// Per-conversation key (usually your dialog ID)
StartCoroutine(dm.actionRunner.RunAction(
    dialogId: "CoffeeAI",
    actionId: "screen.fade",
    payload: "{\"seconds\":1.0}",
    waitForCompletion: true,
    waitSeconds: 0f));


The graph traversal calls these internally when it encounters Action nodes.

PayloadHelper (parsing JSON safely)

PayloadHelper provides helpers to parse payload JSON without writing boilerplate.

A) Strong-typed merge

Define a small class matching your JSON shape:

[System.Serializable]
class FadePayload { public float seconds = 1f; public string color = "#000000"; }

// Merge defaults + JSON
var data = PayloadHelper.MergeJson(new FadePayload { seconds = 0.75f }, payloadJson);
// data.seconds, data.color now populated

B) Typed getters (top-level)
int seconds = PayloadHelper.GetInt(payloadJson, "seconds", 3);       // 3 default
float speed = PayloadHelper.GetFloat(payloadJson, "speed", 1.0f);
bool  wait   = PayloadHelper.GetBool(payloadJson, "wait", false);
string label = PayloadHelper.GetString(payloadJson, "label", "GO!");

C) Colors, vectors, interpolation
// Colors: "#RRGGBB", "r,g,b", or object { "r":255,"g":128,"b":64 }
if (PayloadHelper.TryGetColor(payloadJson, "color", out var c))
    myImage.color = c;

// Vectors: "x,y,z" or object { "x":1, "y":2, "z":0 }
PayloadHelper.TryGetVector3(payloadJson, "pos", out var pos);

// Token interpolation
string text = PayloadHelper.Interpolate("T-{s}", ("s","10")); // "T-10"


Tips

Keep payloads flat (top-level keys) for easy getter access.

Always provide safe defaults in your handler if a key is missing.

Runtime API (selected)
// Start a conversation
void PlayDialogByID(string dialogID, Action onDialogEnded = null);

// History overlay
void PauseForHistory();
void ResumeAfterHistory();

// Autoplay toggle
void ToggleAutoPlay();

// Context
string GetCurrentGuid();
string GetCurrentLineText();

// Events
event Action<string, string, string> OnLineShown;   // nodeGuid, speaker, text
event Action<string, string> OnChoicePicked;        // nodeGuid, choiceText
event Action OnConversationReset;

Editor Tools

Dialog Graph Editor
Create/author graphs; typed nodes (Start/Dialog/Choice/Action/End); node USS styling; character sidebar.

JSON Import/Export
Export a graph to JSON or import JSON into a new graph (good for backups, version control, or AI workflows).

Save Graph Prompt
Safe save with overwrite/rename guarding.

Note: Samples in this build use ScriptableObject graphs (Resources/Conversation/). JSON remains as an editor pipeline option.

Known Notes

If no Action Runner is assigned in DialogManager, Action nodes are skipped (no side effects).

Ensure conversation key in ActionRunner = the Dialog ID you pass to PlayDialogByID(...), otherwise per-conversation handlers/bindings won’t fire (Global can still catch them if useGlobalFallback is on).

waitForCompletion = true blocks traversal until the handler’s coroutine finishes. Use responsibly.