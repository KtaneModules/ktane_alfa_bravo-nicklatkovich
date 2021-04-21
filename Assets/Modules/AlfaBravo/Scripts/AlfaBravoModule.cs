using System.Text.RegularExpressions;
using System.Collections;
using System.Linq;
using UnityEngine;
using KModkit;

public class AlfaBravoModule : MonoBehaviour {
	private const int LETTERS_COUNT = 8;
	private const int MAX_SKIPS_COUNT = 5;
	private const float LETTER_HEIGHT = 0.021f;
	private const float LETTERS_INTERVAL = 0.014f;

	private static int moduleIdCounter = 1;

	private static readonly string[] addendum = new string[LETTERS_COUNT] {
		"LWHTJNFSZO",
		"NFKMUIGVHD",
		"MGIJVFEYSW",
		"CQLYPZUTDX",
		"DTZSBGHFPU",
		"EBRGCHWJNV",
		"GIABZPMQKH",
		"OLSZGUNHRP",
	};

	public GameObject Display;
	public KMAudio Audio;
	public KMBombInfo BombInfo;
	public KMBombModule BombModule;
	public KMSelectable SelfSelectable;
	public KMSelectable SkipButton;
	public Character Stage;
	public Letter LetterPrefab;

	private bool _active = false;
	public bool active {
		get { return _active; }
		private set {
			if (_active == value) return;
			_active = value;
			if (!active) foreach (Letter letter in letters) letter.active = value;
		}
	}

	private int _remainingMinutesCount;
	public int remainingMinutesCount {
		get { return _remainingMinutesCount; }
		private set {
			if (_remainingMinutesCount == value) return;
			_remainingMinutesCount = value;
			if (active) ActualizeLetter(2);
		}
	}

	private int __2faLastDigitSum;
	public int _2faLastDigitsSum {
		get { return __2faLastDigitSum; }
		private set {
			if (__2faLastDigitSum == value) return;
			__2faLastDigitSum = value;
			if (active) ActualizeLetter(3);
		}
	}

	private int _solvedModulesCount;
	public int solvedModulesCount {
		get { return _solvedModulesCount; }
		private set {
			if (_solvedModulesCount == value) return;
			_solvedModulesCount = value;
			if (active) ActualizeLetter(3);
		}
	}

	private int _strikesCount;
	public int strikesCount {
		get { return _strikesCount; }
		private set {
			if (_strikesCount == value) return;
			_strikesCount = value;
			if (active) ActualizeLetter(5);
		}
	}

	private bool _2faPresent;
	private bool activated = false;
	private bool forceSolved = true;
	private bool readyToSkip = false;
	private bool shouldPassOnActivation;
	private bool solved = false;
	private int moduleId;
	private int startingTime;
	private int skipsCount = 0;
	private string TwitchHelpMessage = "\"!{0} 3\" to press letter by its position | \"!{0} skip\" to press button with label SKIP";
	private char[] chars = new char[LETTERS_COUNT];
	private Letter[] letters = new Letter[LETTERS_COUNT];

	private void Start() {
		moduleId = moduleIdCounter++;
		SelfSelectable.Children = new KMSelectable[LETTERS_COUNT + 1];
		for (int i = 0; i < LETTERS_COUNT; i++) {
			Letter letter = Instantiate(LetterPrefab);
			letter.transform.parent = Display.transform;
			float x = LETTERS_INTERVAL * (i - (LETTERS_COUNT - 1) / 2f);
			letter.transform.localPosition = new Vector3(x, LETTER_HEIGHT, 0f);
			letter.transform.localRotation = new Quaternion();
			letter.transform.localScale = Vector3.one;
			letter.Actualized += () => OnLetterActualized();
			KMSelectable letterSelectable = letter.GetComponent<KMSelectable>();
			letterSelectable.Parent = SelfSelectable;
			SelfSelectable.Children[i] = letterSelectable;
			int letterIndex = i;
			letterSelectable.OnInteract += () => { OnLetterPressed(letterIndex); return false; };
			letters[i] = letter;
		}
		SelfSelectable.Children[LETTERS_COUNT] = SkipButton;
		SelfSelectable.UpdateChildren();
		SkipButton.OnInteract += () => { OnSkipPressed(); return false; };
		BombModule.OnActivate += () => Activate();
	}

	private void Update() {
		if (!activated || !active) return;
		remainingMinutesCount = GetRemaingMinutes();
		if (_2faPresent) _2faLastDigitsSum = BombInfo.GetTwoFactorCodes().Select((c) => c % 10).Sum();
		else solvedModulesCount = BombInfo.GetSolvedModuleIDs().Count;
		strikesCount = BombInfo.GetStrikes();
	}

	private void Activate() {
		activated = true;
		readyToSkip = true;
		_2faPresent = BombInfo.IsTwoFactorPresent();
		startingTime = GetRemaingMinutes();
		RandomizeLetters();
		Update();
	}

	private void OnSkipPressed() {
		if (!readyToSkip) return;
		Debug.LogFormat("[Alfa-Bravo #{0}] \"SKIP\" button pressed", moduleId);
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, this.transform);
		if (active && ContainsAnswer()) {
			ProcessAnswer(false);
			Debug.LogFormat("[Alfa-Bravo #{0}] Answer skipped. Strike", moduleId);
		} else if (++skipsCount >= MAX_SKIPS_COUNT) {
			forceSolved = false;
			Debug.LogFormat("[Alfa-Bravo #{0}] {1} skips pressed. Module solved", moduleId, MAX_SKIPS_COUNT);
			ProcessAnswer(true);
		} else RandomizeLetters();
	}

	private bool OnLetterPressed(int index) {
		if (!active) return false;
		bool correct = ButtonIsCorrect(index);
		Debug.LogFormat("[Alfa-Bravo #{0}] Letter #{1} pressed. Answer is {2}", moduleId, index + 1, correct ? "correct" : "wrong");
		if (correct) forceSolved = false;
		ProcessAnswer(correct);
		return correct;
	}

	private void OnLetterActualized() {
		if (active) return;
		for (int j = 0; j < 6; j++) if (!letters[j + 1].actual) return;
		KMBombModule selfBombModule = GetComponent<KMBombModule>();
		if (shouldPassOnActivation) selfBombModule.HandlePass();
		else {
			selfBombModule.HandleStrike();
			readyToSkip = true;
		}
	}

	private void ProcessAnswer(bool solved) {
		shouldPassOnActivation = solved;
		active = false;
		readyToSkip = false;
		letters[0].character = Character.NOT_A_CHARACTER;
		letters[LETTERS_COUNT - 1].character = Character.NOT_A_CHARACTER;
		Stage.character = Character.NOT_A_CHARACTER;
		string message = shouldPassOnActivation ? "SOLVED" : "STRIKE";
		for (int i = 0; i < 6; i++) letters[i + 1].character = message[i];
		if (Enumerable.Range(1, LETTERS_COUNT - 1).All((i) => letters[i].actual)) OnLetterActualized();
	}

	private void RandomizeLetters() {
		Stage.character = Stage.GetRandomCharacter();
		Debug.LogFormat("[Alfa-Bravo #{0}] Small display digit: {1}", moduleId, Stage.character);
		chars = Enumerable.Range(0, LETTERS_COUNT).Select((int _) => (char)Random.Range('A', 'Z' + 1)).ToArray();
		if (Random.Range(0f, 1f) < .3f) {
			int randomIndex = Random.Range(1, LETTERS_COUNT - 1);
			char[] ab = Random.Range(0, 2) == 0 ? new char[] { 'A', 'B' } : new char[] { 'B', 'A' };
			chars[randomIndex] = ab[0];
			chars[randomIndex - 1] = ab[1];
			chars[randomIndex + 1] = ab[1];
		}
		Debug.LogFormat("[Alfa-Bravo #{0}] Generated string: {1}", moduleId, chars.Join(""));
		for (int i = 0; i < LETTERS_COUNT; i++) {
			letters[i].active = true;
			ActualizeLetter(i);
		}
		active = true;
	}

	private void ActualizeLetter(int index) {
		Letter letter = letters[index];
		int addendum = GetLetterAddendum(index);
		int newValue = (chars[index] - 'A' - addendum) % 26;
		if (newValue < 0) newValue = 26 + newValue;
		letter.character = (char)('A' + newValue);
	}

	private IEnumerator ProcessTwitchCommand(string command) {
		command = command.Trim().ToLower();
		if (command.StartsWith("press ")) command = command.Skip(6).Join("").Trim();
		if (Regex.IsMatch(command, @"[1-8]")) {
			if (!active) yield break;
			bool solved = OnLetterPressed(int.Parse(command) - 1);
			yield return null;
			yield return solved ? "solve" : "strike";
			yield break;
		}
		if (command == "skip") {
			yield return null;
			yield return new KMSelectable[] { SkipButton };
		}
		yield break;
	}

	private void TwitchHandleForcedSolve() {
		ProcessAnswer(true);
	}

	private int GetLetterAddendum(int index) {
		char letterAddendum = addendum[index][Stage.character - '0'];
		int valueAddendum = GetValueAddendum(index);
		return letterAddendum - 'A' + valueAddendum;
	}

	private int GetValueAddendum(int index) {
		switch (index) {
			case 0: return BombInfo.GetPortCount();
			case 1: return startingTime;
			case 2: return remainingMinutesCount;
			case 3: return _2faPresent ? _2faLastDigitsSum : solvedModulesCount;
			case 4: return BombInfo.GetSerialNumberNumbers().Sum();
			case 5: return strikesCount + BombInfo.GetModuleIDs().Count;
			case 6: return BombInfo.GetBatteryCount();
			case 7: return BombInfo.GetIndicators().Count();
			default: throw new UnityException("Invalid index provided");
		}
	}

	private bool ButtonIsCorrect(int index) {
		if (index == 0 || index == LETTERS_COUNT - 1) return false;
		char pressedLetter = chars[index];
		if (pressedLetter > 'B') return false;
		char expectedNearLetters = pressedLetter == 'A' ? 'B' : 'A';
		return chars[index - 1] == expectedNearLetters && chars[index + 1] == expectedNearLetters;
	}

	private bool ContainsAnswer() {
		string str = chars.Join("");
		return str.Contains("ABA") || str.Contains("BAB");
	}

	private int GetRemaingMinutes() {
		return Mathf.FloorToInt(BombInfo.GetTime() / 60f);
	}
}
