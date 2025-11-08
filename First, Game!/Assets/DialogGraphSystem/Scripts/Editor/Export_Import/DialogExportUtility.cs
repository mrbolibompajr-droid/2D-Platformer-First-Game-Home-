using System;
using System.Collections.Generic;

namespace DialogSystem.EditorTools.ExportImport
{
    [Serializable]
    public class DialogGraphExport
    {
        public ExportStartNode startNode;
        public ExportEndNode endNode;

        public List<DialogExportDialogNode> dialogNodes = new();
        public List<DialogExportChoiceNode> choiceNodes = new();
        public List<DialogExportActionNode> actionNodes = new();

        public List<ExportLink> links = new();
    }

    [Serializable]
    public class DialogExportDialogNode
    {
        public string title;
        public string guid;
        public string speaker;
        public string question;
        public float nodePositionX;
        public float nodePositionY;
        public float displayTime;
    }

    [Serializable]
    public class DialogExportChoiceNode
    {
        public string guid;
        public string text;
        public float nodePositionX;
        public float nodePositionY;
        public List<ExportChoice> choices = new();
    }

    [Serializable]
    public class ExportChoice
    {
        public string answerText;
        public string nextNodeGUID;
    }

    [Serializable]
    public class ExportLink
    {
        public string fromGuid;
        public string toGuid;
        public int fromPortIndex;
    }

    [Serializable]
    public class ExportStartNode
    {
        public string guid;
        public float nodePositionX;
        public float nodePositionY;
        public bool isInitialized;
    }

    [Serializable]
    public class ExportEndNode
    {
        public string guid;
        public float nodePositionX;
        public float nodePositionY;
        public bool isInitialized;
    }

    [Serializable]
    public class DialogExportActionNode
    {
        public string guid;
        public string actionId;
        public string payloadJson;
        public bool waitForCompletion;
        public float waitSeconds;
        public float nodePositionX;
        public float nodePositionY;
    }
}
