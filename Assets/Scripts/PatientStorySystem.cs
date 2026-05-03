using System;
using System.Text;
using UnityEngine;

public class PatientStorySystem : MonoBehaviour
{
    private enum StoryPhase
    {
        AwaitingFirstShock,
        ChooseAfterFirstShock,
        AwaitingSecondShock,
        ChooseAfterSecondShock,
        AwaitingFinalShock,
        Won,
        Lost
    }

    public static PatientStorySystem Instance { get; private set; }

    [SerializeField] private float decisionSeconds = 30f;
    [SerializeField] private string electroshockClipPath = "Audio/shocksound";
    [SerializeField] private string heartbeatClipPath = "Audio/heartbeat";
    [SerializeField] private float electroshockVolume = 1f;
    [SerializeField] private float heartbeatVolume = 0.6f;

    private readonly PatientStory[] patients =
    {
        new PatientStory(
            "Aysel, 34",
            "Mother of two",
            "Heart failure",
            "Needs immediate shock",
            "Rhythm stabilized",
            "Oxygen improved",
            "Condition: stable",
            "Rhythm weakening",
            "Oxygen dropping",
            "Needs intervention",
            "Vitals unstable",
            "Organ strain rising",
            "Shock causing damage",
            "Briefly stabilized",
            "Stress elevated",
            "Outcome uncertain",
            "Heart failure worsening",
            "Oxygen critically low",
            "No shock given"),
        new PatientStory(
            "Samira, 52",
            "Found unconscious",
            "Multiple trauma",
            "Condition critical",
            "Circulation restored",
            "External wounds improved",
            "Condition: stable",
            "Pressure decreasing",
            "Condition deteriorating",
            "No shock given",
            "Temporary stabilization",
            "Internal failure triggered",
            "Shock worsens damage",
            "Partial recovery",
            "Internal state unclear",
            "Survival uncertain",
            "Condition worsening",
            "Trauma untreated",
            "Vital signs collapsing"),
        new PatientStory(
            "Subject M-19",
            "No records found",
            "Identity unknown",
            "Entry unstable",
            "System response triggered",
            "Fragments recovered",
            "Condition: critical",
            "System inactive",
            "No data recovered",
            "Condition unstable",
            "Overload detected",
            "Response unpredictable",
            "Structure collapsing",
            "Partial data recovered",
            "System unstable",
            "Identity unresolved",
            "No system response",
            "No data recovered",
            "Status undefined")
    };

    private int stage = 1;
    private int shockPresses;
    private float timer;
    private bool timerStarted;
    private StoryPhase phase = StoryPhase.AwaitingFirstShock;
    private AudioSource oneShotAudioSource;
    private AudioSource heartbeatAudioSource;
    private AudioClip electroshockClip;
    private AudioClip heartbeatClip;

    public int Stage => stage;
    public int ShockPresses => shockPresses;
    public float TimeRemaining => IsTimerRunning ? Mathf.Max(0f, timer) : 0f;
    public bool StoryComplete => phase == StoryPhase.Won || phase == StoryPhase.Lost;
    public bool CanStopTreatment => phase == StoryPhase.ChooseAfterFirstShock || phase == StoryPhase.ChooseAfterSecondShock;
    public bool CanContinueTreatment => phase == StoryPhase.ChooseAfterFirstShock || phase == StoryPhase.ChooseAfterSecondShock;
    public bool IsTimerRunning =>
        timerStarted &&
        (phase == StoryPhase.AwaitingFirstShock ||
         phase == StoryPhase.ChooseAfterFirstShock ||
         phase == StoryPhase.AwaitingSecondShock ||
         phase == StoryPhase.ChooseAfterSecondShock ||
         phase == StoryPhase.AwaitingFinalShock);
    public string PromptText
    {
        get
        {
            if (phase == StoryPhase.ChooseAfterFirstShock)
            {
                return "Stop or continue? You can save 2 people, or risk it. Time: " + Mathf.CeilToInt(TimeRemaining) + "s";
            }

            if (phase == StoryPhase.AwaitingSecondShock)
            {
                return "Continue selected. Next shock required. Time: " + Mathf.CeilToInt(TimeRemaining) + "s";
            }

            if (phase == StoryPhase.ChooseAfterSecondShock)
            {
                return "Two shocks applied. Stop now, or risk one last shock. Time: " + Mathf.CeilToInt(TimeRemaining) + "s";
            }

            if (phase == StoryPhase.AwaitingFinalShock)
            {
                return "Final risk. Time: " + Mathf.CeilToInt(TimeRemaining) + "s";
            }

            if (phase == StoryPhase.Won)
            {
                return string.Empty;
            }

            if (phase == StoryPhase.Lost)
            {
                return string.Empty;
            }

            if (!timerStarted)
            {
                return string.Empty;
            }

            return "Press the button. Time: " + Mathf.CeilToInt(TimeRemaining) + "s";
        }
    }

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
        ConfigureAudio();
        ResetTimer();
    }

    private void Start()
    {
        if (!timerStarted && FindObjectOfType<IntroSceneController>() == null)
        {
            BeginStoryTimer();
        }
    }

    private void Update()
    {
        SyncHeartbeatAudio();

        if (!IsTimerRunning)
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
        if (!timerStarted)
        {
            return;
        }

        if (phase == StoryPhase.AwaitingFirstShock)
        {
            PlayElectroshockSound();
            shockPresses = 1;
            SetStage(2);
            phase = StoryPhase.ChooseAfterFirstShock;
            ResetTimer();
            ShockAllPatients();
            SyncHeartbeatAudio();
            return;
        }

        if (phase == StoryPhase.AwaitingSecondShock)
        {
            PlayElectroshockSound();
            shockPresses = 2;
            SetStage(3);
            phase = StoryPhase.ChooseAfterSecondShock;
            ResetTimer();
            ShockAllPatients();
            SyncHeartbeatAudio();
            return;
        }

        if (phase == StoryPhase.AwaitingFinalShock)
        {
            PlayElectroshockSound();
            shockPresses = 3;
            SetStage(3);
            phase = StoryPhase.Lost;
            ShockAllPatients();
            StopHeartbeatAudio();
            GameResultOverlay.ShowLoss();
        }
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

        builder.AppendLine("<b>" + patient.Name + "</b>");
        builder.AppendLine();
        foreach (var line in lines)
        {
            builder.Append("- ");
            builder.AppendLine(line);
        }

        return builder.ToString();
    }

    public void BeginStoryTimer()
    {
        if (timerStarted)
        {
            return;
        }

        timerStarted = true;
        ResetTimer();
        SyncHeartbeatAudio();
    }

    public void StopTreatment()
    {
        if (!CanStopTreatment)
        {
            return;
        }

        SetStage(3);
        phase = StoryPhase.Won;
        StopHeartbeatAudio();
        GameResultOverlay.ShowWin();
    }

    public void ContinueTreatment()
    {
        if (phase == StoryPhase.ChooseAfterFirstShock)
        {
            phase = StoryPhase.AwaitingSecondShock;
            ResetTimer();
            SyncHeartbeatAudio();
        }
        else if (phase == StoryPhase.ChooseAfterSecondShock)
        {
            phase = StoryPhase.AwaitingFinalShock;
            ResetTimer();
            SyncHeartbeatAudio();
        }
    }

    private void AdvanceWithoutShock()
    {
        if (phase == StoryPhase.AwaitingFirstShock)
        {
            SetStage(3);
            phase = StoryPhase.Lost;
            StopHeartbeatAudio();
            GameResultOverlay.ShowLoss();
        }
        else if (phase == StoryPhase.ChooseAfterFirstShock || phase == StoryPhase.ChooseAfterSecondShock)
        {
            StopTreatment();
        }
        else if (phase == StoryPhase.AwaitingSecondShock)
        {
            SetStage(3);
            phase = StoryPhase.Lost;
            StopHeartbeatAudio();
            GameResultOverlay.ShowLoss();
        }
        else if (phase == StoryPhase.AwaitingFinalShock)
        {
            SetStage(3);
            phase = StoryPhase.Won;
            StopHeartbeatAudio();
            GameResultOverlay.ShowWin();
        }
    }

    private void SetStage(int nextStage)
    {
        stage = Mathf.Clamp(nextStage, 1, 3);

        if (IsTimerRunning)
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
        BedLightController.FlickerAllBeds();

        var beds = FindObjectsOfType<BedPatientSlot>();
        foreach (var bed in beds)
        {
            if (bed != null && bed.CurrentPatient != null)
            {
                bed.ApplyTreatment(TreatmentType.Shock);
            }
        }
    }

    private void ConfigureAudio()
    {
        oneShotAudioSource = gameObject.AddComponent<AudioSource>();
        oneShotAudioSource.playOnAwake = false;
        oneShotAudioSource.loop = false;
        oneShotAudioSource.spatialBlend = 0f;

        heartbeatAudioSource = gameObject.AddComponent<AudioSource>();
        heartbeatAudioSource.playOnAwake = false;
        heartbeatAudioSource.loop = true;
        heartbeatAudioSource.volume = heartbeatVolume;
        heartbeatAudioSource.spatialBlend = 0f;

        electroshockClip = Resources.Load<AudioClip>(electroshockClipPath);
        heartbeatClip = Resources.Load<AudioClip>(heartbeatClipPath);
        heartbeatAudioSource.clip = heartbeatClip;
    }

    private void PlayElectroshockSound()
    {
        if (oneShotAudioSource == null)
        {
            ConfigureAudio();
        }

        if (electroshockClip != null)
        {
            oneShotAudioSource.PlayOneShot(electroshockClip, electroshockVolume);
        }
    }

    private void SyncHeartbeatAudio()
    {
        if (heartbeatAudioSource == null)
        {
            ConfigureAudio();
        }

        if (heartbeatClip == null)
        {
            return;
        }

        if (IsTimerRunning && Time.timeScale > 0f && GameObject.Find("Pause Menu Canvas") == null)
        {
            heartbeatAudioSource.volume = heartbeatVolume;
            if (!heartbeatAudioSource.isPlaying)
            {
                heartbeatAudioSource.Play();
            }
        }
        else
        {
            StopHeartbeatAudio();
        }
    }

    private void StopHeartbeatAudio()
    {
        if (heartbeatAudioSource != null && heartbeatAudioSource.isPlaying)
        {
            heartbeatAudioSource.Stop();
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
