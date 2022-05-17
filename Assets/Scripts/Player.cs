using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Player : Actor
{
    const string PlayerHUDPath = "Prefabs/PlayerHUD";

    /// <summary>
    /// 이동할 벡터
    /// </summary>
    [SerializeField]
    [SyncVar]
    Vector3 moveVector = Vector3.zero;

    [SerializeField]
    NetworkIdentity NetworkIdentity = null;

    /// <summary>
    /// 이동 속도
    /// </summary>
    [SerializeField]
    float speed;

    [SerializeField]
    BoxCollider boxCollider;

    [SerializeField]
    Transform fireTransform;

    [SerializeField]
    float bulletSpeed = 1;

    InputController inputController = new InputController();

    [SerializeField]
    [SyncVar]
    bool Host = false; // Host 플레이어인지 여부   

    [SerializeField]
    Material ClientPalyerMaterial;

    [SerializeField]
    [SyncVar]
    int UsableItemCount = 0;

    public int ItemCount
    {
        get
        {
            return UsableItemCount;
        }
    }

    private void Start()
    {
        CurrentHP = MaxHP;

        if (isServer)
        {
            actorInstanceID = GetInstanceID();
            RpcSetActorInstanceID(actorInstanceID);
        }       

        FirstWingerSceneMain firstWingerSceneMain = SystemManager.Instance.GetCurrentSceneMain<FirstWingerSceneMain>();

        if (isLocalPlayer)
            firstWingerSceneMain.Hero = this;
        else
            firstWingerSceneMain.OtherPlayer = this;

        if (isServer && isLocalPlayer)
        {
            Host = true;
            RpcSetHost();
        }

        Transform startTransform;
        if (!Host)             
        {         
            MeshRenderer meshRenderer = GetComponentInChildren<MeshRenderer>();
            meshRenderer.material = ClientPalyerMaterial;
        }     

        if (actorInstanceID != 0)
            firstWingerSceneMain.ActorManager.Regist(actorInstanceID, this);

        InitializePlayerHUD();
    }

    void InitializePlayerHUD()
    {
        FirstWingerSceneMain firstWingerSceneMain = SystemManager.Instance.GetCurrentSceneMain<FirstWingerSceneMain>();
        GameObject go = Resources.Load<GameObject>(PlayerHUDPath);
        GameObject goInstance = Instantiate<GameObject>(go, Camera.main.WorldToScreenPoint(transform.position), Quaternion.identity, firstWingerSceneMain.DamageManager.CanvasTransform);
        PlayerHUD playerHUD = goInstance.GetComponent<PlayerHUD>();
        playerHUD.Initialize(this);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("OnStartClient");
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        Debug.Log("OnStartLocalPlayer");      
    }

    protected override void UpdateActor()
    {
        if (!isLocalPlayer)
            return;

        UpdateInput();
        UpdateMove();
    }
    
    [ClientCallback]
    public void UpdateInput()
    {
        inputController.UpdateInput();
    }  

    void UpdateMove()
    {
        if (moveVector.sqrMagnitude == 0)
            return;

        // 정상적으로 NetworkBehaviour 인스턴스의 Update로 호출되어 실행되고 있을 때
        //CmdMove(MoveVector);

        // MonoBehaviour 인스턴스의 Update로 호출되어 실행되고 있을 때의 꼼수
        // 이 경우 클라이언트로 접속하면 Command로 보내지지만 자기자신은 CmdMove를 실행 못함
        if (isServer)
        {
            RpcMove(moveVector); // Host 플레이어인 경우 RPC로 보내고
        }
        else
        {
            CmdMove(moveVector); // Client 플레이어인 경우 Cmd로 호스트로 보낸 후 자신을 Self 동작
            if (isLocalPlayer)
                transform.position += AdjustMoveVector(moveVector);
        }
    }

    [Command]
    public void CmdMove(Vector3 moveVector)
    {
        this.moveVector = moveVector;
        transform.position += AdjustMoveVector(this.moveVector);
        base.SetDirtyBit(1);
        this.moveVector = Vector3.zero; // 타 플레이어가 보낸 경우 Update를 통해 초기화 되지 않으므로 사용 후 바로 초기화
    }

    [ClientRpc]
    public void RpcMove(Vector3 moveVector)
    {
        this.moveVector = moveVector;
        transform.position += AdjustMoveVector(this.moveVector);
        base.SetDirtyBit(1);
        this.moveVector = Vector3.zero; // 타 플레이어가 보낸 경우 Update를 통해 초기화 되지 않으므로 사용 후 바로 초기화
    }

    public void ProcessInput(Vector3 moveDirection)
    {
        moveVector = moveDirection * speed * Time.deltaTime;
    }

    Vector3 AdjustMoveVector(Vector3 moveVector)
    {
        Transform mainBGQuadTransform = SystemManager.Instance.GetCurrentSceneMain<FirstWingerSceneMain>().MainBGQuadTransform;

        Vector3 result = Vector3.zero;

        result = boxCollider.transform.position + boxCollider.center + moveVector;

        if (result.x - boxCollider.size.x * 0.5f < -mainBGQuadTransform.localScale.x * 0.5f)
            moveVector.x = 0;

        if (result.x + boxCollider.size.x * 0.5f > mainBGQuadTransform.localScale.x * 0.5f)
            moveVector.x = 0;

        if (result.y - boxCollider.size.y * 0.5f < -mainBGQuadTransform.localScale.y * 0.5f)
            moveVector.y = 0;

        if (result.y + boxCollider.size.y * 0.5f > mainBGQuadTransform.localScale.y * 0.5f)
            moveVector.y = 0;

        return moveVector;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("other = " + other.name);

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy)
        {
            if (!enemy.IsDead)
            {
                BoxCollider box = ((BoxCollider)other);
                Vector3 crashPos = enemy.transform.position + box.center;
                crashPos.x += box.size.x * 0.5f;

                enemy.OnCrash(CrashDamage, crashPos);
            }
        }
    }  

    public void Fire()
    {
        if(Host)
        {
            Bullet bullet = SystemManager.Instance.GetCurrentSceneMain<FirstWingerSceneMain>().BulletManager.Genelate(BulletManager.PlayerBulletIndex, fireTransform.position);
            bullet.Fire(actorInstanceID, fireTransform.right, bulletSpeed, damage);
        }
        else
        {
            CmdFire(actorInstanceID, fireTransform.position, fireTransform.right, bulletSpeed, damage);
        }
    }

    [Command]
    public void CmdFire(int ownerInstanceID, Vector3 firePosition, Vector3 direction, float speed, int damage)
    {
        Bullet bullet = SystemManager.Instance.GetCurrentSceneMain<FirstWingerSceneMain>().BulletManager.Genelate(BulletManager.PlayerBulletIndex, firePosition);
        bullet.Fire(ownerInstanceID, direction, speed, damage);
        base.SetDirtyBit(1);
    }

    public void FireBomb()
    {
        if (UsableItemCount <= 0)
            return;

        if (Host)
        {
            Bullet bullet = SystemManager.Instance.GetCurrentSceneMain<FirstWingerSceneMain>().BulletManager.Genelate(BulletManager.PlayerBombIndex, fireTransform.position);
            bullet.Fire(actorInstanceID, fireTransform.right, bulletSpeed, bombDamage);
        }
        else
        {
            CmdFireBomb(actorInstanceID, fireTransform.position, fireTransform.right, bulletSpeed, bombDamage);
        }

        DecreaseUsableItemCount();
    }

    [Command]
    public void CmdFireBomb(int ownerInstanceID, Vector3 firePosition, Vector3 direction, float speed, int bombDamage)
    {
        Bullet bullet = SystemManager.Instance.GetCurrentSceneMain<FirstWingerSceneMain>().BulletManager.Genelate(BulletManager.PlayerBombIndex, firePosition);
        bullet.Fire(ownerInstanceID, direction, speed, bombDamage);
        base.SetDirtyBit(1);
    }

    void DecreaseUsableItemCount()
    {
        // 정상적으로 NetworkBehaviour 인스턴스의 Update로 호출되어 실행되고 있을 때
        //CmdSetPosition(position);

        // MonoBehaviour 인스턴스의 Update로 호출되어 실행되고 있을 때의 꼼수
        if (isServer)
        {
            RpcDecreaseUsableItemCount(); // Host 플레이어인 경우 RPC로 보내고
        }
        else
        {
            CmdDecreaseUsableItemCount(); // Client 플레이어인 경우 Cmd로 호스트로 보낸 후 자신
            if (isLocalPlayer)
                UsableItemCount--;
        }
    }

    [Command]
    public void CmdDecreaseUsableItemCount()
    {
        UsableItemCount--;
        base.SetDirtyBit(1);
    }

    [ClientRpc]
    public void RpcDecreaseUsableItemCount()
    {
        UsableItemCount--;
        base.SetDirtyBit(1);
    }

    protected override void DecreaseHP(int value, Vector3 damagePos)
    {
        base.DecreaseHP(value, damagePos);      

        Vector3 damagePoint = damagePos + Random.insideUnitSphere * 0.5f;
        SystemManager.Instance.GetCurrentSceneMain<FirstWingerSceneMain>().DamageManager.Generate(DamageManager.PlayerDamageIndex, damagePoint, value, Color.red);
    }

    protected override void OnDead()
    {
        base.OnDead();
        gameObject.SetActive(false);      
    }

    [ClientRpc]
    public void RpcSetHost()
    {
        Host = true;
        base.SetDirtyBit(1);
    }

    protected virtual void InternalIncreaseHP(int value)
    {
        if (isDead)
            return;

        CurrentHP += value;

        if (CurrentHP > MaxHP)
            CurrentHP = MaxHP;     
    }

    public virtual void IncreaseHP(int value)
    {
        if (isDead)
            return;

        CmdIncreaseHP(value);
    }

    [Command]
    public void CmdIncreaseHP(int value)
    {
        InternalIncreaseHP(value);
        base.SetDirtyBit(1);
    }

    public virtual void IncreaseUsableItem(int value = 1)
    {
        if (isDead)
            return;

        CmdIncreseUsableItem(value);
    }

    [Command]
    public virtual void CmdIncreseUsableItem(int value)
    {
        UsableItemCount += value;       
        base.SetDirtyBit(1);
    }
}
