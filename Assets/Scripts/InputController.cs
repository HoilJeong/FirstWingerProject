using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class InputController
{
    //[SyncVar]
    //protected float LastActionUpdateTime = 0.0f;

    //const float ActionUpdateInterval = 0.1f;

    // Update is called once per frame
    public void UpdateInput()
    {
        if (SystemManager.Instance.GetCurrentSceneMain<FirstWingerSceneMain>().CurrentGameState != GameState.Running)
            return;
        

        UpdateMove();
        UpdateAttack();
    }

    void UpdateMove()
    {
        Vector3 moveDirection = Vector3.zero;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            moveDirection.y = 1;
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            moveDirection.y = -1;
        }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            moveDirection.x = -1;
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            moveDirection.x = 1;
        }

        SystemManager.Instance.GetCurrentSceneMain<FirstWingerSceneMain>().Hero.ProcessInput(moveDirection);
    }

    void UpdateAttack()
    {
        
        if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyUp(KeyCode.X))
        {
            SystemManager.Instance.GetCurrentSceneMain<FirstWingerSceneMain>().Hero.Fire();
        }
        

        if (Input.GetKeyDown(KeyCode.Z))
        {
            SystemManager.Instance.GetCurrentSceneMain<FirstWingerSceneMain>().Hero.FireBomb();
        }
    }
}
