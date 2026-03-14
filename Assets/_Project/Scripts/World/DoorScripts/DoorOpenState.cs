using System.Collections;
using UnityEngine;

public class DoorOpenState : DoorState
{
    private Coroutine autoCloseCoroutine;

    public DoorOpenState(DoorStateMachine machine) : base(machine) { }

    public override void Enter()
    {
        // zvuk a animace už proběhly v DoorOpeningState
        machine.Controller.SetPromptEnabled(false);

        if (machine.Lock.EnableAutoClose)
            autoCloseCoroutine = machine.StartCoroutine(AutoCloseCoroutine());
    }

    public override void Exit()
    {
        if (autoCloseCoroutine != null)
        {
            machine.StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }
    }

    private IEnumerator AutoCloseCoroutine()
    {
        yield return new WaitForSeconds(machine.Lock.AutoCloseDelay);
        machine.SetState(new DoorClosingState(machine));
    }

    public override void Interact()
    {
        machine.SetState(new DoorClosingState(machine));
    }
}