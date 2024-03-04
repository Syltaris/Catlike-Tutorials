using System;
using UnityEngine;

public class Fractal : MonoBehaviour {

	[SerializeField, Range(1, 8)]
	int depth = 4;

    [SerializeField]
	Mesh mesh;

	[SerializeField]
	Material material;


    /*
    At minimum we need to know the direction and rotation of a part. We could keep track of those by storing them in arrays. 
    But instead of using separate arrays for vectors and quaternions we'll group them together, by creating a new FractalPart struct type. 
    This is done like defining a class, but with the struct keyword instead of class. 
    As we'll only need this type inside Fractal define it inside that class, along with its fields. Don't make it public, for the same reason.
    */
    struct FractalPart {
		public Vector3 direction;
		public Quaternion rotation;
        public Transform transform;
	}

    FractalPart[][] parts;


	static Vector3[] directions = {
		Vector3.up, Vector3.right, Vector3.left, Vector3.forward, Vector3.back
	};

	static Quaternion[] rotations = {
		Quaternion.identity,
		Quaternion.Euler(0f, 0f, -90f), Quaternion.Euler(0f, 0f, 90f),
		Quaternion.Euler(90f, 0f, 0f), Quaternion.Euler(-90f, 0f, 0f)
	};

	FractalPart CreatePart (int levelIndex, int childIndex, float scale) {
		var go = new GameObject("Fractal Part L" + levelIndex + " C" + childIndex);
        go.transform.localScale = scale * Vector3.one;
		go.transform.SetParent(transform, false);

		go.AddComponent<MeshFilter>().mesh = mesh;
        go.AddComponent<MeshRenderer>().material = material;

        return new FractalPart {
            direction = directions[childIndex],
			rotation = rotations[childIndex],
			transform = go.transform
        };
	}

    void Awake () {
		parts = new FractalPart[depth][];
		parts[0] = new FractalPart[1]; // Each level gets its own array, also the root level of the fractal that has only a single part. So begin by creating a new FractalPart array for a single element and assign it to the first level.
		
        for (int i = 0, length = 1; i < parts.Length; i++, length *= 5) { // go through roots depths
			parts[i] = new FractalPart[length]; // each depth has depth^i nodes
		}

		float scale = 1f;
		parts[0][0] = CreatePart(0, 0, scale);
        for (int li = 1; li < parts.Length; li++) {
            scale *= 0.5f;
            FractalPart[] levelParts = parts[li];
			for (int fpi = 0; fpi < levelParts.Length; fpi += 5) {
				for (int ci = 0; ci < 5; ci++) {
					levelParts[fpi + ci] = CreatePart(li, ci, scale);
				}
			}
        }
	}

    void Update () {
		Quaternion deltaRotation = Quaternion.Euler(0f, 22.5f * Time.deltaTime, 0f);

		FractalPart rootPart = parts[0][0];
		rootPart.rotation *= deltaRotation;
		rootPart.transform.localRotation = rootPart.rotation;
		parts[0][0] = rootPart;

		for (int li = 1; li < parts.Length; li++) {
            FractalPart[] parentParts = parts[li - 1];
			FractalPart[] levelParts = parts[li];


			for (int fpi = 0; fpi < levelParts.Length; fpi++) {
                Transform parentTransform = parentParts[fpi / 5].transform;
				FractalPart part = levelParts[fpi];

				part.rotation *= deltaRotation;

				part.transform.localRotation = parentTransform.localRotation * part.rotation; // stacked rotations
                part.transform.localPosition =
					parentTransform.localPosition +
					parentTransform.localRotation * // affect offset before extension
					(1.5f * part.transform.localScale.x * part.direction);

				levelParts[fpi] = part;
			}
		}
	}
}