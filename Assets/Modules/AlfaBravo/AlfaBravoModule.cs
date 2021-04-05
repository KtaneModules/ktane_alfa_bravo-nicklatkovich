using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlfaBravoModule : MonoBehaviour {
	private const int LETTERS_COUNT = 8;
	private const float LETTER_HEIGHT = 0.021f;
	private const float LETTERS_INTERVAL = 0.014f;

	public GameObject display;
	public KMSelectable skipButton;
	public Character stage;
	public Letter letterPrefab;

	public readonly Letter[] letters = new Letter[LETTERS_COUNT];
	public int skipsToAnswer;

	private void Start() {
		skipsToAnswer = Random.Range(0, 4);
		KMSelectable selfSelectable = GetComponent<KMSelectable>();
		selfSelectable.Children = new KMSelectable[LETTERS_COUNT + 1];
		for (int i = 0; i < LETTERS_COUNT; i++) {
			Letter letter = Instantiate(letterPrefab);
			letter.transform.parent = display.transform;
			float x = LETTERS_INTERVAL * (i - (LETTERS_COUNT - 1) / 2f);
			letter.transform.localPosition = new Vector3(x, LETTER_HEIGHT, 0f);
			KMSelectable letterSelectable = letter.GetComponent<KMSelectable>();
			letterSelectable.Parent = selfSelectable;
			selfSelectable.Children[i] = letterSelectable;
			int letterIndex = i;
			letterSelectable.OnInteract += () => {
				PressLetter(letterIndex);
				return false;
			};
			letters[i] = letter;
		}
		selfSelectable.Children[LETTERS_COUNT] = skipButton;
		selfSelectable.UpdateChildren();
		GetComponent<KMBombModule>().OnActivate += () => RandomizeLetters();
		skipButton.OnInteract += () => {
			RandomizeLetters();
			return false;
		};
	}

	public void PressLetter(int index) {
		bool? correct = ButtonIsCorrect(index);
		if (correct == null) return;
		if (correct == true) {
			letters[0].character = Character.NOT_A_CHARACTER;
			letters[LETTERS_COUNT - 1].character = Character.NOT_A_CHARACTER;
			for (int i = 0; i < 6; i++) letters[i + 1].character = "SOLVED"[i];
			stage.character = Character.NOT_A_CHARACTER;
			GetComponent<KMBombModule>().HandlePass();
			return;
		}
		GetComponent<KMBombModule>().HandleStrike();
	}

	public void RandomizeLetters() {
		foreach (Letter letter in letters) letter.character = letter.GetRandomCharacter();
		if (skipsToAnswer <= 0) {
			skipsToAnswer = Random.Range(0, 4);
			int candidateIndex = Random.Range(1, LETTERS_COUNT - 1);
			char[] ab = Random.Range(0, 2) == 0 ? new char[] { 'A', 'B' } : new char[] { 'B', 'A' };
			letters[candidateIndex].character = ab[0];
			letters[candidateIndex - 1].character = ab[1];
			letters[candidateIndex + 1].character = ab[1];
		} else skipsToAnswer -= 1;
		stage.character = stage.GetRandomCharacter();
	}

	public bool? ButtonIsCorrect(int index) {
		Letter letter = letters[index];
		if (!letter.actual) return null;
		if (index == 0 || index == LETTERS_COUNT - 1) return false;
		char pressedLetter = letter.character;
		if (pressedLetter > 'B') return false;
		char expectedNearLetters = pressedLetter == 'A' ? 'B' : 'A';
		Letter leftLetter = letters[index - 1];
		if (!leftLetter.actual || leftLetter.character != expectedNearLetters) return false;
		Letter rightLetter = letters[index + 1];
		if (!rightLetter.actual || rightLetter.character != expectedNearLetters) return false;
		return true;
	}
}
