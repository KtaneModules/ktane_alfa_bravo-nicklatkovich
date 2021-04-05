using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Letter : Character {
	private bool _highlighted = false;
	public bool highlighted {
		get { return _highlighted; }
		private set {
			if (_highlighted == value) return;
			_highlighted = value;
			UpdateMeshColor();
		}
	}

	protected override void Start() {
		base.Start();
		KMSelectable selfSelectable = GetComponent<KMSelectable>();
		selfSelectable.OnHighlight += () => highlighted = true;
		selfSelectable.OnHighlightEnded += () => highlighted = false;
	}

	public override Color GetMeshColor() {
		return activeCharacter != character ? Color.blue : (highlighted ? Color.red : Color.white);
	}
}
