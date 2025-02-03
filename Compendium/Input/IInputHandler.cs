using UnityEngine;

namespace Compendium.Input;

public interface IInputHandler
{
	KeyCode Key { get; }

	bool IsChangeable { get; }

    string Id { get; }

    string Label { get; }

    void OnPressed(ReferenceHub player);
}
