using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Enemy : Actor
{
    public enum State : int
    { 
        None = -1, // 사용전
        Ready = 0, // 준비완료
        Appear, // 등장
        Battle, // 전투
        Dead, // 사망
        Disappear // 퇴장
    }

    /// <summary>
    /// 현재 상태값
    /// </summary>
    [SerializeField]
    [SyncVar]
    State currentState = State.None;


    /// <summary>
    /// 최고 속도
    /// </summary>
    protected const float maxSpeed = 10.0f;

    const float maxSpeedTime = 0.5f;

    [SerializeField]
    [SyncVar]
    protected Vector3 targetPosition;

    [SerializeField]
    [SyncVar]
    protected float currentSpeed;

    /// <summary>
    /// 방향을 고려한 속도 벡터
    /// </summary>
    [SyncVar]
    protected Vector3 currentVelocity;

    [SyncVar]
    protected float moveStartTime = 0.0f;   

    [SerializeField]
    protected Transform fireTransform;

    [SerializeField]
    [SyncVar]
    float bulletSpeed = 1;

    [SyncVar]
    protected float LastActionUpdateTime = 0.0f;

    [SerializeField]
    [SyncVar]
    protected int fireRemainCount = 1;

    [SerializeField]
    [SyncVar]
    int gamePoint = 10;

    [SyncVar]
    [SerializeField]
    string filePath;

    public string FilePath
    {
        get
        {
            return filePath;
        }
        set
        {
            filePath = value;
        }
    }
    [SyncVar]
    Vector3 AppearPoint; // 입장시 도착 위치

    [SyncVar]
    Vector3 DisappearPoint; // 퇴장시 목표 위치

    [SerializeField]
    [SyncVar]
    float ItemDropRate; // 아이템 생성 확률

    [SerializeField]
    [SyncVar]
    int ItemDropID;  // 아이템 생성시 참조할 ItemDrop 테이블의 인덱스

    protected virtual int BulletIndex
    {
        get
        {
            return BulletManager.EnemyBulletIndex;
        }
    }

    protected override void UpdateActor()
    {
        switch (currentState)
        {
            case State.None:
                break;

            case State.Ready:
                UpdateReady();
                break;

            case State.Dead:
                break;

            case State.Appear:

            case State.Disappear:
                UpdateSpeed();
                UpdateMove();
                break;

            case State.Battle:
                UpdateBattle();
                break;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        CurrentHP = MaxHP;

        if (isServer)
        {
            actorInstanceID = GetInstanceID();
            RpcSetActorInstanceID(actorInstanceID);
        }

        FirstWingerSceneMain firstWingerSceneMain = SystemManager.Instance.GetCurrentSceneMain<FirstWingerSceneMain>();

        if (!((FWNetworkManager)FWNetworkManager.singleton).isServer)
        {
            transform.SetParent(firstWingerSceneMain.EnemyManager.transform);
            firstWingerSceneMain.EnemyCacheSystem.Add(FilePath, gameObject);
            gameObject.SetActive(false);
        }

        Debug.Log("Enemy: Initialize");

        if (actorInstanceID != 0)
            firstWingerSceneMain.ActorManager.Regist(actorInstanceID, this);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateActor();               
    }

    protected void UpdateSpeed()
    {
        // CurrentSpeed 에서 MaxSpeed 에 도달하는 비율을 흐른 시간만큼 계산
        currentSpeed = Mathf.Lerp(currentSpeed, maxSpeed, (Time.time - moveStartTime) / maxSpeedTime);
    }

    void UpdateMove()
    {
        float distance = Vector3.Distance(targetPosition, transform.position);
        if (distance == 0)
        {
            Arrived();
            return;
        }

        // 이동벡터 계산. 양 벡터의 차를 통해 이통벡터를 구한 후 nomalized 로 단위벡터 계산
        currentVelocity = (targetPosition - transform.position).normalized * currentSpeed;

        // 자연스러운 감속으로 목표지점에 도착할 수 있도록 계산
        // 속도 = 거리 / 시간 이므로 시간 = 거리 / 속도
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, distance / currentSpeed, maxSpeed);
    }

    void Arrived()
    {
        currentSpeed = 0.0f;

        if (currentState == State.Appear)
        {
            SetBattleState();
        }
        else // if (currentState == State.disappear)
        {
            currentState = State.None;
            SystemManager.Instance.GetCurrentSceneMain<FirstWingerSceneMain>().EnemyManager.RemoveEnemy(this);
        }
    }

    protected virtual void SetBattleState()
    {
        currentState = State.Battle;
        LastActionUpdateTime = Time.time;
    }

    public void Reset(SquadronMemberStruct data)
    {
        // 정상적으로 NetworkBehaviour 인스턴스의 Update로 호출되어 실행되고 있을 때
        //CmdReset(data);

        // MonoBehaviour 인스턴스의 Update로 호출되어 실행되고 있을 때의 꼼수
        if (isServer)
        {
            RpcReset(data); // Host 플레이어인 경우 RPC로 보내고
        }
        else
        {
            CmdReset(data); // Client 플레이어인 경우 Cmd로 호스트로 보낸 후 자신
            if (isLocalPlayer)
                ResetData(data);
        }
    }

    public void ResetData(SquadronMemberStruct data)
    {
        EnemyStruct enemyStruct = SystemManager.Instance.EnemyTable.GetEnemy(data.EnemyID);

        CurrentHP = MaxHP = enemyStruct.MaxHP;         // currentHP까지 다시 입력
        damage = enemyStruct.Damage;                   // 총알 데미지
        crashDamage = enemyStruct.CrashDamage;         // 충돌 데미지
        bulletSpeed = enemyStruct.BulletSpeed;         // 총알 속도
        fireRemainCount = enemyStruct.FireRemainCount; // 발사할 총알 갯수
        gamePoint = enemyStruct.GamePoint;             // 파괴시 플레이어가 얻을 점수

        AppearPoint = new Vector3(data.AppearPointX, data.AppearPointY, 0);          // 입장시 도착 위치
        DisappearPoint = new Vector3(data.DisappearPointX, data.DisappearPointY, 0); // 퇴장시 목표 위치

        ItemDropRate = enemyStruct.ItemDropRate; // 아이템 생성 확률
        ItemDropID = enemyStruct.ItemDropID; // 아이템 Drop 테이블 참조

        currentState = State.Ready;
        LastActionUpdateTime = Time.time;

        isDead = false; // Enemy는 재사용되므로 초기화시켜줘야 함
    }

    public void Appear(Vector3 targetPos)
    {
        targetPosition = targetPos;
        currentSpeed = maxSpeed; // 나타날때는 최고 스피드로 설정

        currentState = State.Appear;
        moveStartTime = Time.time;
    }

    void Disapper(Vector3 targetPos)
    {
        targetPosition = targetPos;
        currentSpeed = 0;

        currentState = State.Disappear;
        moveStartTime = Time.time;
    }

    void UpdateReady()
    {
        if (Time.time - LastActionUpdateTime > 1.0f)
        {
            Appear(AppearPoint);
        }
    }

    protected virtual void UpdateBattle()
    {
        if (Time.time - LastActionUpdateTime > 1.0f)
        {
            if (fireRemainCount > 0)
            {
                Fire();
                fireRemainCount--;
            }
            else
            {
                Disapper(DisappearPoint);
            }
            LastActionUpdateTime = Time.time;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("other =" + other.name);
        Player player = other.GetComponentInParent<Player>();
        if (player)
        {
            if (!player.IsDead)
            {
                BoxCollider box = ((BoxCollider)other);
                Vector3 crashPos = player.transform.position + box.center;
                crashPos.x += box.size.x * 0.5f;

                player.OnCrash(CrashDamage, crashPos);
            }
        }
    }
   

    public void Fire()
    {


        Bullet bullet = SystemManager.Instance.GetCurrentSceneMain<FirstWingerSceneMain>().BulletManager.Genelate(BulletIndex, fireTransform.position);
        if (bullet)       
            bullet.Fire(actorInstanceID, -fireTransform.right, bulletSpeed, damage);
    }

    protected override void OnDead()
    {
        base.OnDead();

        FirstWingerSceneMain firstWingerSceneMain = SystemManager.Instance.GetCurrentSceneMain<FirstWingerSceneMain>();
        firstWingerSceneMain.GamePointAccumulator.Accumulate(gamePoint);
        firstWingerSceneMain.EnemyManager.RemoveEnemy(this);

        GenerateItem();

        currentState = State.Dead;
        
    }

    protected override void DecreaseHP(int value, Vector3 damagePos)
    {
        base.DecreaseHP(value, damagePos);

        Vector3 damagePoint = damagePos + Random.insideUnitSphere * 0.5f;
        SystemManager.Instance.GetCurrentSceneMain<FirstWingerSceneMain>().DamageManager.Generate(DamageManager.EnemyDamageIndex, damagePoint, value, Color.magenta);
    }

    [Command]
    public void CmdReset(SquadronMemberStruct data)
    {
        ResetData(data);
        base.SetDirtyBit(1);
    }

    [ClientRpc]
    public void RpcReset(SquadronMemberStruct data)
    {
        ResetData(data);
        base.SetDirtyBit(1);
    }

    void GenerateItem()
    {
        if (!isServer)
            return;

        // 아이템 생성 확률을 검사
        float ItemGen = Random.Range(0.0f, 100.0f);
        if (ItemGen > ItemDropRate)
            return;

        ItemDropTable itemDropTable = SystemManager.Instance.ItemDropTable;
        ItemDropStruct dropStruct = itemDropTable.GetDropData(ItemDropID);

        // 어느 아이템을 생성할 것인지 확률 검사
        ItemGen = Random.Range(0, dropStruct.Rate1 + dropStruct.Rate2 + dropStruct.Rate3);
        int ItemIndex = -1;

        if (ItemGen <= dropStruct.Rate1) // 1번 아이템 비율보다 작은 경우
            ItemIndex = dropStruct.ItemID1;
        else if (ItemGen <= (dropStruct.Rate1 + dropStruct.Rate2)) // 2번 아이템 비율보다 작은 경우
            ItemIndex = dropStruct.ItemID2;
        else //if (ItemGen <= (dropStruct.Rate1 + dropStruct.Rate2 + dropStruct.Rate3)) // 3번 아이템 비율인 경우
            ItemIndex = dropStruct.ItemID3;

        Debug.Log("GenerateItem ItemIndex = " + ItemIndex);

        FirstWingerSceneMain firstWingerSceneMain = SystemManager.Instance.GetCurrentSceneMain<FirstWingerSceneMain>();
        firstWingerSceneMain.ItemBoxManager.Generate(ItemIndex, transform.position);
    }

    public void AddList()
    {
        if (isServer)
            RpcAddList(); // Host 플레이어인 경우 RPC로 보내고
    }

    [ClientRpc]
    public void RpcAddList()
    {
        SystemManager.Instance.GetCurrentSceneMain<FirstWingerSceneMain>().EnemyManager.AddList(this);
        base.SetDirtyBit(1);
    }

    public void RemoveList()
    {
        if (isServer)
            RpcRemoveList(); // Host 플레이어인 경우 RPC로 보내고
    }

    [ClientRpc]
    public void RpcRemoveList()
    {
        SystemManager.Instance.GetCurrentSceneMain<FirstWingerSceneMain>().EnemyManager.RemoveList(this);
        base.SetDirtyBit(1);
    }
}
