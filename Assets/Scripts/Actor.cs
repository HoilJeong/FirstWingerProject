using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Actor : NetworkBehaviour
{
    [SerializeField]
    [SyncVar]
    protected int MaxHP = 100;

    public int HPMax
    {
        get
        {
            return MaxHP;
        }
    }

    [SerializeField]
    [SyncVar]
    protected int CurrentHP;

    public int HPCurrnet
    {
        get
        {
            return CurrentHP;
        }
    }

    [SerializeField]
    [SyncVar]
    protected int damage = 1;   

    [SerializeField]
    [SyncVar]
    protected int crashDamage = 100;

    [SerializeField]
    [SyncVar]
    protected int bombDamage = 1;

    [SerializeField]
    [SyncVar]
    protected bool isDead = false;

    public bool IsDead
    {
        get
        {
            return isDead;
        }
    }

    protected int CrashDamage
    {
        get
        {
            return crashDamage;
        }
    }

    [SyncVar]
    protected int actorInstanceID = 0;
    public int ActorInstanceID
    {
        get
        {
            return actorInstanceID;
        }
    }   

    // Start is called before the first frame update
    void Start()
    {
        
    } 

    // Update is called once per frame
    void Update()
    {
        UpdateActor();
    }

    protected virtual void UpdateActor()
    {

    }

    public virtual void OnBulletHited(int damage, Vector3 hitPos)
    {
        Debug.Log("OnBulletHited damage = " + damage);
        DecreaseHP(damage, hitPos);
    }

    public virtual void OnBombHited(int damage, Vector3 hitPos)
    {
        Debug.Log("OnBulletBombHited damage = " + damage);
        DecreaseHP(damage, hitPos);
    }

    public virtual void OnCrash(int damage, Vector3 crashPos)
    {
        Debug.Log("OnCrash damage = " + damage);
        DecreaseHP(damage, crashPos);
    }

    protected virtual void DecreaseHP(int value, Vector3 damagePos)
    {
        if (isDead)
            return;     

        // MonoBehaviour 인스턴스의 Update로 호출되어 실행되고 있을 때의 꼼수      
        if (isServer)
        {
            RpcDecreaseHP(value, damagePos); // Host 플레이어인 경우 RPC로 보내고
        }      
    }

    protected virtual void InternalDecreaseHP(int value, Vector3 damagePos)
    {
        if (isDead)
            return;

        CurrentHP -= value;

        if (CurrentHP < 0)
            CurrentHP = 0;

        if (CurrentHP == 0)
        {
            OnDead();
        }
    }   

    protected virtual void OnDead()
    {
        Debug.Log(name + "OnDead");
        isDead = true;

        SystemManager.Instance.GetCurrentSceneMain<FirstWingerSceneMain>().EffectManager.GenerateEffect(EffectManager.ActorDeadFxIndex, transform.position);
    }

    public void SetPosition(Vector3 position)
    {
        // 정상적으로 NetworkBehaviour 인스턴스의 Update로 호출되어 실행되고 있을 때
        //CmdSetPosition(position);

        // MonoBehaviour 인스턴스의 Update로 호출되어 실행되고 있을 때의 꼼수
        if (isServer)
        {
            RpcSetPosition(position); // Host 플레이어인 경우 RPC로 보내고
        }
        else
        {
            CmdSetPosition(position); // Client 플레이어인 경우 Cmd로 호스트로 보낸 후 자신
            if (isLocalPlayer)
                transform.position = position;
        }
    }

    [Command]
    public void CmdSetPosition(Vector3 position)
    {
        this.transform.position = position;
        base.SetDirtyBit(1);
    }

    [ClientRpc]
    public void RpcSetPosition(Vector3 position)
    {
        this.transform.position = position;
        base.SetDirtyBit(1);
    }

    [ClientRpc]
    public void RpcSetActive(bool value)
    {
        this.gameObject.SetActive(value);
        base.SetDirtyBit(1);
    }

    [ClientRpc]
    public void RpcSetActorInstanceID(int instID)
    {
        this.actorInstanceID = instID;

        if (this.actorInstanceID != 0)
            SystemManager.Instance.GetCurrentSceneMain<FirstWingerSceneMain>().ActorManager.Regist(this.actorInstanceID, this);

        base.SetDirtyBit(1);
    }

    [Command]
    public void CmdDecreaseHP(int value, Vector3 damagePos)
    {
        InternalDecreaseHP(value, damagePos);
        base.SetDirtyBit(1);
    }

    [ClientRpc]
    public void RpcDecreaseHP(int value, Vector3 damagePos)
    {
        InternalDecreaseHP(value, damagePos);
        base.SetDirtyBit(1);
    }
}
