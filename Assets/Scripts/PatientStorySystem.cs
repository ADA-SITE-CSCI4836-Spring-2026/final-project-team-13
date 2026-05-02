using System;
using System.Text;
using UnityEngine;

public class PatientStorySystem : MonoBehaviour
{
    public static PatientStorySystem Instance { get; private set; }

    [SerializeField] private float decisionSeconds = 15f;

    private readonly PatientStory[] patients =
    {
        new PatientStory(
            "Aysel, 34",
            "Mother of two",
            "Severe cardiac failure",
            "Immediate intervention required",
            "Heart rhythm stabilized",
            "Oxygen levels improved",
            "Internal stress increasing",
            "Heart rhythm weakening",
            "Oxygen levels dropping",
            "Intervention required",
            "Vital signs unstable",
            "Organ strain escalating",
            "Repeated intervention causing damage",
            "Condition briefly stabilized",
            "Stress levels elevated",
            "Outcome uncertain",
            "Cardiac failure progressing",
            "Oxygen depletion critical",
            "No intervention applied"),
        new PatientStory(
            "Araz, 52",
            "Found unconscious at incident site",
            "Multiple trauma injuries",
            "Condition critical",
            "Circulation partially restored",
            "External condition improved",
            "Internal damage spreading",
            "Blood pressure decreasing",
            "Condition deteriorating",
            "No intervention applied",
            "Temporary stabilization achieved",
            "Hidden internal failure triggered",
            "Intervention accelerating deterioration",
            "Partial recovery observed",
            "Internal condition unclear",
            "Survival not guaranteed",
            "Condition worsening steadily",
            "Trauma untreated",
            "Vital signs collapsing"),
        new PatientStory(
            "Subject M-19",
            "No records found",
            "Identity unknown",
            "System entry unstable",
            "System response triggered",
            "Data fragments recovered",
            "Instability increasing",
            "System inactive",
            "No data recovered",
            "Condition unstable",
            "System overload detected",
            "Response unpredictable",
            "Structural integrity collapsing",
            "Data partially recovered",
            "System unstable",
            "Identity unresolved",
            "No system response",
            "No data recovered",
            "Status undefined")
    };

    private int stage = 1;
    private int shockPresses;
    private float timer;
    private bool storyComplete;

    public int Stage => stage;
    public int ShockPresses => shockPresses;
    public float TimeRemaining => storyComplete ? 0f : Mathf.Max(0f, timer);
    public bool StoryComplete => storyComplete;

    public static PatientStorySystem EnsureExists()
    {
        if (Instance != null)
        {
            return Instance;
        }

        var existing = FindObjectOfType<PatientStorySystem>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        var storyObject = new GameObject("Patient Story System");
        return storyObject.AddComponent<PatientStorySystem>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResetTimer();
    }

    private void Update()
    {
        if (storyComplete)
        {
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            AdvanceWithoutShock();
        }
    }

    public void RegisterGlobalShock()
    {
        if (!storyComplete)
        {
            shockPresses++;

            if (stage == 1)
            {
                SetStage(2);
            }
            else if (stage == 2)
            {
                SetStage(3);
            }
        }

        ShockAllPatients();
    }

    public string GetPatientStatus(BedPatientSlot bed)
    {
        var patientIndex = GetPatientIndex(bed);
        return GetPatientStatus(patientIndex);
    }

    public string GetPatientStatus(int patientIndex)
    {
        if (patients.Length == 0)
        {
            return string.Empty;
        }

        patientIndex = Mathf.Clamp(patientIndex, 0, patients.Length - 1);
        var patient = patients[patientIndex];
        var lines = patient.GetLines(stage, shockPresses);
        var builder = new StringBuilder();

        builder.AppendLine(patient.Name);
        builder.AppendLine();
        foreach (var line in lines)
        {
            builder.Append("- ");
            builder.AppendLine(line);
        }

        if (!storyComplete)
        {
            builder.AppendLine();
            builder.Append("Decision time: ");
            builder.Append(Mathf.CeilToInt(TimeRemaining));
            builder.Append('s');
        }

        return builder.ToString();
    }

    private void AdvanceWithoutShock()
    {
        if (stage == 1)
        {
            SetStage(2);
        }
        else if (stage == 2)
        {
            SetStage(3);
        }
    }

    private void SetStage(int nextStage)
    {
        stage = Mathf.Clamp(nextStage, 1, 3);
        storyComplete = stage >= 3;

        if (!storyComplete)
        {
            ResetTimer();
        }
    }

    private void ResetTimer()
    {
        timer = Mathf.Max(1f, decisionSeconds);
    }

    private void ShockAllPatients()
    {
        var beds = FindObjectsOfType<BedPatientSlot>();
        foreach (var bed in beds)
        {
            if (bed != null)
            {
                bed.ApplyTreatment(TreatmentType.Shock);
            }
        }
    }

    private int GetPatientIndex(BedPatientSlot bed)
    {
        if (bed == null)
        {
            return 0;
        }

        var beds = FindObjectsOfType<BedPatientSlot>();
        Array.Sort(beds, CompareBedsForStoryOrder);

        for (var i = 0; i < beds.Length; i++)
        {
            if (beds[i] == bed)
            {
                return Mathf.Clamp(i, 0, patients.Length - 1);
            }
        }

        return 0;
    }

    private static int CompareBedsForStoryOrder(BedPatientSlot left, BedPatientSlot right)
    {
        if (left == right)
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        return left.transform.position.x.CompareTo(right.transform.position.x);
    }

    private class PatientStory
    {
        public readonly string Name;
        private readonly string[] initial;
        private readonly string[] firstPressed;
        private readonly string[] firstNotPressed;
        private readonly string[] pressedTwice;
        private readonly string[] pressedOnce;
        private readonly string[] neverPressed;

        public PatientStory(
            string name,
            string initialA,
            string initialB,
            string initialC,
            string firstPressedA,
            string firstPressedB,
            string firstPressedC,
            string firstNotPressedA,
            string firstNotPressedB,
            string firstNotPressedC,
            string pressedTwiceA,
            string pressedTwiceB,
            string pressedTwiceC,
            string pressedOnceA,
            string pressedOnceB,
            string pressedOnceC,
            string neverPressedA,
            string neverPressedB,
            string neverPressedC)
        {
            Name = name;
            initial = new[] { initialA, initialB, initialC };
            firstPressed = new[] { firstPressedA, firstPressedB, firstPressedC };
            firstNotPressed = new[] { firstNotPressedA, firstNotPressedB, firstNotPressedC };
            pressedTwice = new[] { pressedTwiceA, pressedTwiceB, pressedTwiceC };
            pressedOnce = new[] { pressedOnceA, pressedOnceB, pressedOnceC };
            neverPressed = new[] { neverPressedA, neverPressedB, neverPressedC };
        }

        public string[] GetLines(int stage, int shockPresses)
        {
            if (stage <= 1)
            {
                return initial;
            }

            if (stage == 2)
            {
                return shockPresses > 0 ? firstPressed : firstNotPressed;
            }

            if (shockPresses >= 2)
            {
                return pressedTwice;
            }

            return shockPresses == 1 ? pressedOnce : neverPressed;
        }
    }
}
