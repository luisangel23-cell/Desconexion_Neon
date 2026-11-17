using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem.UI;
#endif

// Keeps the samples working whichever Active Input Handling the project uses.
// The scenes ship with a StandaloneInputModule, which reads UnityEngine.Input; when the
// project only has the Input System package enabled, swap it for the Input System UI module.
[RequireComponent(typeof(EventSystem))]
public class InputModuleSelector : MonoBehaviour
{
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
	void Awake ()
	{
		// StandaloneInputModule and TouchInputModule both read UnityEngine.Input.
		// Destroy them immediately so the EventSystem never ticks them this frame.
		foreach (var module in GetComponents<PointerInputModule>())
			DestroyImmediate(module);

		if (GetComponent<InputSystemUIInputModule>() == null)
			gameObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
	}
#endif
}
