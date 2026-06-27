using UnityEngine;

public class TrapEffectStub : MonoBehaviour, ITrapEffect
{
    [SerializeField] private string message = "Trap effect is not implemented yet.";

    public void Execute(TrapContext context)
    {
        GameEvents.OnTrapStubRequested?.Invoke(new TrapStubRequest(context.Position, message, context.Trap));
    }
}
