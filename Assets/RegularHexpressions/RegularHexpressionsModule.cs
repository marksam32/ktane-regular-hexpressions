using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RegularHexpressionsModule : MonoBehaviour
{
	public KMBombInfo Bomb;
	public KMBombModule Module;
	public KMAudio Audio;

	public GameObject topWordText;
	public GameObject initialCoordinateText;

	// Must be in the same order as Hexahedron.Vertex
	public GameObject[] vertexObjects;

	// Must be in the same order as Hexahedron.Face
	public KMSelectable faceButtonU;
	public KMSelectable faceButtonD;
	public KMSelectable faceButtonF;
	public KMSelectable faceButtonB;
	public KMSelectable faceButtonL;
	public KMSelectable faceButtonR;

	public KMSelectable submitButton;

	private RegularHexpressionsController controller;

	private Dictionary<Hexahedron.Vertex, Vector3> vertexPositions;

	private static int _moduleIdCounter = 1;
	private int _moduleId;

	void Start()
	{
		_moduleId = _moduleIdCounter++;

		controller = new RegularHexpressionsController(Bomb, LogPrefix());

		vertexPositions = new Dictionary<Hexahedron.Vertex, Vector3>();
		for (int vertexIndex = 0; vertexIndex < vertexObjects.Length; vertexIndex++) {
			vertexPositions[(Hexahedron.Vertex)vertexIndex] = vertexObjects[vertexIndex].transform.localPosition;
		}

		faceButtonU.OnInteract += GetFaceButtonPressHandler(Hexahedron.Face.UP, faceButtonU);
		faceButtonD.OnInteract += GetFaceButtonPressHandler(Hexahedron.Face.DOWN, faceButtonD);
		faceButtonF.OnInteract += GetFaceButtonPressHandler(Hexahedron.Face.FRONT, faceButtonF);
		faceButtonB.OnInteract += GetFaceButtonPressHandler(Hexahedron.Face.BACK, faceButtonB);
		faceButtonL.OnInteract += GetFaceButtonPressHandler(Hexahedron.Face.LEFT, faceButtonL);
		faceButtonR.OnInteract += GetFaceButtonPressHandler(Hexahedron.Face.RIGHT, faceButtonR);

		submitButton.OnInteract += GetSubmitButtonPressHandler(submitButton);

		Render();
	}

	private KMSelectable.OnInteractHandler GetFaceButtonPressHandler(Hexahedron.Face face, KMSelectable button)
	{
		return delegate {
			button.AddInteractionPunch();
			Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
			if (controller.StartFaceTurn(face)) {
				Render();
				StartCoroutine(FaceTurnAnimation(face));
			}
			return false;
		};
	}

	private KMSelectable.OnInteractHandler GetSubmitButtonPressHandler(KMSelectable button)
	{
		return delegate {
			button.AddInteractionPunch();
			Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);

			switch (controller.Submit()) {
			case RegularHexpressionsController.SubmitResult.FAILURE:
				Module.HandleStrike();
				Render();
				break;
			case RegularHexpressionsController.SubmitResult.SUCCESS:
				Module.HandlePass();
				Render();
				break;
			}
			return false;
		};
	}

	private struct VertexAnimationData
	{
		public GameObject vertexObject;
		public Vector3 startPosition;
		public Vector3 endPosition;
	}

	private IEnumerator FaceTurnAnimation(Hexahedron.Face face)
	{
		Hexahedron.Vertex[] faceVertices = Hexahedron.VERTICES_BY_FACE[(int)face];
		VertexAnimationData[] animationData = faceVertices.Select((vertex, faceVertexIndex) => new VertexAnimationData {
			vertexObject = vertexObjects[(int)controller.vertexPermutationInverse[vertex]],
			startPosition = vertexPositions[vertex],
			endPosition = vertexPositions[faceVertices[(faceVertexIndex + 1) % faceVertices.Length]],
		}).ToArray();

		float elapsedTime = 0f;
		float duration = 0.6f;
		while (elapsedTime < duration) {
			yield return null;
			elapsedTime += Time.deltaTime;
			foreach (VertexAnimationData ad in animationData) {
				float progress = Mathf.Min(1f, elapsedTime / duration);
				ad.vertexObject.transform.localPosition = Vector3.Lerp(ad.startPosition, ad.endPosition, progress);
			}
		}

		// Ensure vertices stop at exactly the correct position
		foreach (VertexAnimationData ad in animationData) {
			ad.vertexObject.transform.localPosition = ad.endPosition;
		}

		controller.FinishFaceTurn();
		Render();
	}

	private void Render()
	{
		topWordText.GetComponent<TextMesh>().text = controller.GetTopWord();
		topWordText.GetComponent<MeshRenderer>().enabled = !controller.TopWordIsMoving();

		initialCoordinateText.GetComponent<TextMesh>().text = controller.GetInitialCoordinates();
		initialCoordinateText.GetComponent<MeshRenderer>().enabled = controller.TopWordIsMoving();
	}

	private string LogPrefix()
	{
		return string.Format("[Regular Hexpressions #{0}]", _moduleId);
	}
	
#pragma warning disable 414
	private const string TwitchHelpMessage =
		"Use !{0} press U D F B L R TM BM BL BR TL TR, to press the button(s) corresponding to that face of the hexahedron, or the button's position. Use !{0} cycle U B R TM TL TR, to cycle the corresponding face(s), or use the button's position. You can cycle U, B, or R. Submit your answer using !{0} submit.";
#pragma warning restore 414
	
	private IEnumerator ProcessTwitchCommand(string command)
	{
		command = command.ToLowerInvariant().Trim();
	
		if (command.EqualsAny("submit", "press submit", "check"))
		{
			yield return null;
			submitButton.OnInteract();
			yield break;
		}
	
		if (command.StartsWith("press"))
		{
			var parts = command.Split(' ');
			var selectables = new List<KMSelectable>();
	
			for (int i = 1; i < parts.Length; i++)
			{
				switch (parts[i])
				{
					case "u":
					case "tm":
						selectables.Add(faceButtonU);
						break;
					case "d":
					case "bm":
						selectables.Add(faceButtonD);
						break;
					case "f":
					case "br":
						selectables.Add(faceButtonF);
						break;
					case "b":
					case "tl":
						selectables.Add(faceButtonB);
						break;
					case "l":
					case "bl":
						selectables.Add(faceButtonL);
						break;
					case "r":
					case "tr":
						selectables.Add(faceButtonR);
						break;
					default:
						yield return string.Format("sendtochaterror Do you honestly expect me to know what {0} means? 4Head",
							parts[i].ToUpperInvariant());
						yield break;
				}
			}

			if (!selectables.Any())
			{
				yield return "sendtochaterror Maybe you should tell me what to press. Kappa";
				yield break;
			}

			yield return null;
			
			foreach (var selectable in selectables)
			{
				yield return "trycancel";
				selectable.OnInteract();
				yield return new WaitForSeconds(.8f);
			}
			
			yield break;
		}

		if (command.StartsWith("cycle"))
		{
			var parts = command.Split(' ');
			var selectables = new List<KMSelectable>();
			
			for (int i = 1; i < parts.Length; i++)
			{
				switch (parts[i])
				{
					case "u":
					case "tm":
						selectables.Add(faceButtonU);
						break;
					case "b":
					case "tl":
						selectables.Add(faceButtonB);
						break;
					case "r":
					case "tr":
						selectables.Add(faceButtonR);
						break;
					default:
						yield return string.Format("sendtochaterror Sorry, I can't cycle the {0} face.",
							parts[i].ToUpperInvariant());
						yield break;
				}
			}
			
			if (!selectables.Any())
			{
				yield return "sendtochaterror Maybe you should tell me what to cycle. Kappa";
				yield break;
			}

			yield return null;

			foreach (var selectable in selectables)
			{
				for (int i = 0; i < 4; i++)
				{
					yield return new WaitForSeconds(3f);
					selectable.OnInteract();
					yield return "trycancel";
				}
				yield return new WaitForSeconds(1f);
				yield return "trycancel";
			}
		}
	}
}
