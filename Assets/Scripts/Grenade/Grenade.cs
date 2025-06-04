using UnityEngine;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit; // XRGrabInteractableのイベント利用のため

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))] // XRGrabInteractableが同じオブジェクトにあることを必須とする
public class Grenade : MonoBehaviour
{
    [Header("State")]
    private bool hasPinPulled = false;
    private bool isExploded = false;

    [Header("Explosion Settings")]
    [Tooltip("爆発がターゲットを検知する半径")]
    public float explosionRadius = 5.0f;
    [Tooltip("ターゲット近接検知用のトリガーコライダー（SphereColliderを想定）")]
    public SphereCollider proximityTrigger; // インスペクターで設定
    // public float explosionForce = 700f; // 他のRigidbodyに力を加える場合（今回はターゲット破壊のみ）

    [Header("Effects (Assign in Inspector)")]
    [Tooltip("爆発時に生成するパーティクルエフェクトのプレハブ")]
    public GameObject explosionEffectPrefab;
    [Tooltip("ピン抜き時の効果音")]
    public AudioClip pinPullSound;
    [Tooltip("爆発時の効果音")]
    public AudioClip explosionSound;
    // private AudioSource audioSource; // AudioSourceコンポーネントで音を再生する場合

    [Header("Socket & Despawn Settings")]
    [Tooltip("手から離れた後、自動消滅するまでの時間（秒）")]
    public float despawnTimeWhenDropped = 15.0f;
    [Tooltip("ソケットから物理的に離れたとみなす距離の閾値(の2乗)")]
    public float dislodgementDistanceThresholdSqr = 0.01f; // 例: 10cm四方

    private GrenadeSocket originalSocket;
    private bool wasPickedFromSocket = false; // ソケットから一度でも持ち上げられた/離脱したか
    private Coroutine timedDespawnCoroutine = null;
    private Coroutine dislodgementCheckCoroutine = null; // ★追加: 物理的離脱検知用コルーチン

    private XRGrabInteractable grabInteractable; // XRGrabInteractableへの参照
    private Rigidbody rb; // Rigidbodyへの参照

    private bool isPinBeingHeld = false; // ピンが現在掴まれているか

    private Coroutine delayedKinematicChangeCoroutine = null;
    [Tooltip("ピンを離してから本体のKinematicをfalseにするまでの遅延時間（秒）")]
    private float delayBeforeNonKinematicAfterPinRelease = 0.1f; // 短い遅延時間

    void Awake()
    {
        // audioSource = GetComponent<AudioSource>(); // AudioSourceがない場合は追加する
        // if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        rb = GetComponent<Rigidbody>();

        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            Debug.LogError("Grenade: XRGrabInteractableコンポーネントが見つかりません！このスクリプトと同じGameObjectに必要です。", this);
            enabled = false;
            return;
        }

        // XRGrabInteractableのイベントを購読
        grabInteractable.selectEntered.AddListener(HandleSelectEntered);
        grabInteractable.selectExited.AddListener(HandleSelectExited);

        if (proximityTrigger == null)
        {
            // 必須ではないが、警告を出す（近接爆発が機能しないため）
            Debug.LogWarning("Grenade: Proximity Triggerが設定されていません。ターゲット近接による爆発は機能しません。", this);
        }
        else
        {
            proximityTrigger.isTrigger = true; // 念のためトリガーに設定
            proximityTrigger.radius = explosionRadius; // 爆発半径とトリガー半径を一致させる (または別途設定)
        }
    }

    void Start()
    {
        // GrenadeManagerに自身を登録
        if (GrenadeManager.Instance != null)
        {
            // 本来はInstantiateする側がCanSpawnGrenade()を確認すべきだが、
            // ここでは、万が一上限を超えて生成された場合に自身を破棄するなどのフォールバックも考えられる
            // （今回は、生成側が責任を持つ前提で、単純に登録のみ）
            GrenadeManager.Instance.RegisterGrenade();
        }
        else
        {
            Debug.LogError("Grenade: GrenadeManagerのインスタンスが見つかりません！", this);
        }
        // 初期状態では近接検知を無効（InitializeInSocketで再度設定する可能性あり）
        if (proximityTrigger != null) proximityTrigger.enabled = false;
    }

    /// <summary>
    /// GrenadeSocketSpawnerから呼び出され、どのソケットに配置されたかを初期設定します。
    /// </summary>
    public void InitializeInSocket(GrenadeSocket socket)
    {
        originalSocket = socket;
        wasPickedFromSocket = false; // まだソケットから持ち上げられていない/離脱していない

        /* if (rb != null)
        {
            rb.isKinematic = true;
            // rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        } */

        if (proximityTrigger != null) proximityTrigger.enabled = false; // ソケット内では近接検知無効

        // ★物理的離脱を監視するコルーチンを開始
        if (dislodgementCheckCoroutine != null) StopCoroutine(dislodgementCheckCoroutine);
        dislodgementCheckCoroutine = StartCoroutine(CheckIfDislodgedRoutine());
    }

    // ★ソケットからの物理的離脱を監視するコルーチン
    private IEnumerator CheckIfDislodgedRoutine()
    {
        if (originalSocket == null || originalSocket.spawnPoint == null)
        {
            // Debug.LogWarning("Dislodgement check cannot start: originalSocket or spawnPoint is null.");
            dislodgementCheckCoroutine = null;
            yield break;
        }

        Vector3 initialSocketPosition = originalSocket.spawnPoint.position;

        while (originalSocket != null && !wasPickedFromSocket && !isExploded && !grabInteractable.isSelected)
        {
            // isSelected のチェックを追加: プレイヤーが持っている間はこのコルーチンでの離脱判定は不要
            if ((transform.position - initialSocketPosition).sqrMagnitude > dislodgementDistanceThresholdSqr)
            {
                Debug.Log($"Grenade {gameObject.name} dislodged from socket {originalSocket.gameObject.name} by external force.");
                ProcessSocketDeparture(); // ソケット離脱処理を共通化

                // 物理的に離脱した場合、物理演算を有効にする
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                }

                // 手に持たれていないので、ドロップ後の消滅タイマーを開始
                if (timedDespawnCoroutine != null) StopCoroutine(timedDespawnCoroutine);
                timedDespawnCoroutine = StartCoroutine(DespawnAfterDelayRoutine());
                Debug.Log("Grenade dislodged from socket, starting despawn timer.");

                dislodgementCheckCoroutine = null; // コルーチン終了
                yield break;
            }
            yield return new WaitForSeconds(0.25f); // チェック間隔
        }
        dislodgementCheckCoroutine = null; // 通常終了
    }

    // ★ソケット離脱処理を共通化するメソッド
    private void ProcessSocketDeparture()
    {
        if (originalSocket != null)
        {
            originalSocket.OnGrenadeTaken();
            wasPickedFromSocket = true; // これがtrueになることで、ドロップ後の消滅対象になる
            originalSocket = null; // ソケットとの関連を切る
            Debug.Log($"Grenade {gameObject.name} processed departure from socket.");
        }

        // 物理的離脱監視コルーチンは不要になるので停止
        if (dislodgementCheckCoroutine != null)
        {
            StopCoroutine(dislodgementCheckCoroutine);
            dislodgementCheckCoroutine = null;
        }
    }

    private void HandleSelectEntered(SelectEnterEventArgs args)
    {
        if (!wasPickedFromSocket && originalSocket != null) // まだソケットから離脱処理がされていない場合
        {
            ProcessSocketDeparture(); // プレイヤーによるピックアップもソケット離脱として処理
        }

        // もしピン離脱後のKinematic変更遅延コルーチンが動いていたら停止
        if (delayedKinematicChangeCoroutine != null)
        {
            StopCoroutine(delayedKinematicChangeCoroutine);
            delayedKinematicChangeCoroutine = null;
            // Debug.Log("Grenade grabbed, stopping any delayed kinematic change.");
        }

        if (timedDespawnCoroutine != null)
        {
            StopCoroutine(timedDespawnCoroutine);
            timedDespawnCoroutine = null;
            Debug.Log("Dropped grenade picked up, despawn timer reset.");
        }
    }

    private void HandleSelectExited(SelectExitEventArgs args)
    {
        if (wasPickedFromSocket && !isExploded)
        {
            // ★★★ 離された瞬間に物理挙動を開始させる ★★★
            if (rb != null && rb.isKinematic)
            {
                rb.isKinematic = false;
                // Gravityは手を離した後、自動で使われるので
                // UseGravityをonにする必要はない
            }
            Debug.Log($"Grenade {gameObject.name} released. Kinematic: {rb.isKinematic}, Gravity: {rb.useGravity}");

            if (timedDespawnCoroutine != null) StopCoroutine(timedDespawnCoroutine);
            timedDespawnCoroutine = StartCoroutine(DespawnAfterDelayRoutine());
            Debug.Log("Grenade dropped, starting despawn timer.");
        }
    }

    private IEnumerator DespawnAfterDelayRoutine()
    {
        yield return new WaitForSeconds(despawnTimeWhenDropped);

        // grabInteractable.isSelected は XRGrabInteractable が選択中かを示すプロパティ
        if (!isExploded && (grabInteractable == null || !grabInteractable.isSelected))
        {
            Debug.Log($"Grenade {gameObject.name} despawned after being dropped for {despawnTimeWhenDropped} seconds.");
            Destroy(gameObject);
        }
        else
        {
            // Debug.Log($"Despawn for {gameObject.name} cancelled (exploded or re-selected).");
        }
        timedDespawnCoroutine = null;
    }

    /// <summary>
    /// GrenadePinから呼び出され、ピンとのインタラクション状態を更新します。
    /// </summary>
    /// <param name="isPinHeld">ピンが現在持たれているか</param>
    public void NotifyPinInteractionState(bool isPinHeld)
    {
        if (isExploded || hasPinPulled) return; // 既に処理済みなら何もしない

        bool previousPinState = isPinBeingHeld;
        isPinBeingHeld = isPinHeld;

        if (grabInteractable.isSelected) // グレネード本体が現在誰かに持たれている場合のみ影響
        {
            // 既存の遅延Kinematic変更コルーチンがあれば停止
            if (delayedKinematicChangeCoroutine != null)
            {
                StopCoroutine(delayedKinematicChangeCoroutine);
                delayedKinematicChangeCoroutine = null;
            }
            if (isPinBeingHeld)
            {
                // ピンが持たれたら、グレネード本体をKinematicにして安定させる
                rb.isKinematic = true;
                // rb.useGravity = false; // isKinematic=trueなら重力は効かないが、念のため
                // Debug.Log("Pin is being held, Grenade body set to Kinematic.");
            }
            else
            {
                // ピンが離されたら、少し遅れて本体の物理を戻す
                if (previousPinState == true) // 直前までピンが持たれていた場合のみ遅延処理を開始
                {
                    delayedKinematicChangeCoroutine = StartCoroutine(MakeNonKinematicAfterDelayRoutine());
                }
                // Debug.Log("Pin released (but not pulled), Grenade body physics potentially restored by XRI (should be non-kinematic).");
            }
        }
    }

    private IEnumerator MakeNonKinematicAfterDelayRoutine()
    {
        yield return new WaitForSeconds(delayBeforeNonKinematicAfterPinRelease);

        // このコルーチンが開始された後、ピンが再度掴まれたり、グレネードが捨てられたり、
        // ピンが完全に抜かれたりしていないか確認する。
        // isPinBeingHeld: ピンが再度掴まれていれば、Kinematicのままにするべき
        // !grabInteractable.isSelected: グレネードが捨てられていれば、HandleSelectExitedで物理状態が処理される
        // hasPinPulled: ピンが抜かれていれば、OnPinPulledで物理状態が処理される
        if (!isPinBeingHeld && grabInteractable.isSelected && !hasPinPulled && !isExploded)
        {
            rb.isKinematic = false;
            // Velocity Trackingの場合、掴んでいる間はuseGravityはXRIに任せる(通常false)
            // rb.useGravity = false; // Velocity Tracking中はXRIがこれを制御
            Debug.Log($"Grenade body made non-kinematic after pin release delay. Kinematic: {rb.isKinematic}");
        }
        else
        {
            // Debug.Log("Delayed non-kinematic change cancelled due to state change (pin re-held, grenade dropped, pin pulled, or exploded).");
        }
        delayedKinematicChangeCoroutine = null;
    }

    /// <summary>
    /// GrenadePinから呼び出され、ピンが抜かれたことを処理します。
    /// </summary>
    public void OnPinPulled()
    {
        if (isExploded || hasPinPulled) return; // 既に爆発済みかピンが抜かれていれば何もしない

        // ピンが抜かれる前に動いていた可能性のある遅延Kinematic変更コルーチンを停止
        if (delayedKinematicChangeCoroutine != null)
        {
            StopCoroutine(delayedKinematicChangeCoroutine);
            delayedKinematicChangeCoroutine = null;
        }

        hasPinPulled = true;
        isPinBeingHeld = false; // ピンが抜かれたら、もう持たれている状態ではない
        Debug.Log("グレネードのピンが抜かれ、爆発準備完了。");

        if (rb != null)
        {
            rb.isKinematic = false; // ピンが抜けたら確実に非Kinematic
            // もし本体がまだ掴まれていない状態でピンが抜かれたら（ありえないが）、重力ON
            if (!grabInteractable.isSelected)
            {
                rb.useGravity = true;
            }
        }

        // ピン抜き効果音再生
        if (pinPullSound != null /*&& audioSource != null*/)
        {
            // audioSource.PlayOneShot(pinPullSound);
            AudioSource.PlayClipAtPoint(pinPullSound, transform.position); // 簡易的な再生
        }

        // (オプション) ピンが抜けたら近接検知トリガーを有効にするなど
        if (proximityTrigger != null)
        {
            proximityTrigger.enabled = true;
        }

        // ピンが抜かれた時点で、ソケットとの関連は明確に切る
        // ProcessSocketDepartureはwasPickedFromSocketもtrueにするので、
        // wasPickedFromSocketがfalseのままでもソケットを解放できるように、
        // originalSocketがnullでないなら直接呼び出す。
        if (originalSocket != null) 
        {
            ProcessSocketDeparture(); // これによりoriginalSocket = nullになり、wasPickedFromSocket = trueになる
        }
    }

    // ターゲットが近接検知トリガーに入ったときに呼び出される
    void OnTriggerEnter(Collider other)
    {
        if (!hasPinPulled || isExploded) return; // ピンが抜かれていないか、既に爆発済みなら何もしない

        // ターゲットかどうかを確認 (PooledTargetコンポーネントを持つかなど)
        PooledTarget target = other.GetComponent<PooledTarget>();
        if (target != null)
        {
            // Debug.Log("ターゲット (" + other.name + ") がグレネードの近接範囲に入りました。爆発します。");
            Explode();
        }
    }

    private void Explode()
    {
        if (isExploded) return;
        isExploded = true;

        Debug.Log("グレネード爆発！");

        // 確実に時間自動消滅コルーチンを停止
        if (timedDespawnCoroutine != null)
        {
            StopCoroutine(timedDespawnCoroutine);
            timedDespawnCoroutine = null;
        }

        // 物理的離脱監視コルーチンも停止
        if (dislodgementCheckCoroutine != null)
        {
            StopCoroutine(dislodgementCheckCoroutine);
            dislodgementCheckCoroutine = null;
        }

        // 爆発エフェクト生成
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        // 爆発音再生
        if (explosionSound != null /*&& audioSource != null*/)
        {
            // audioSource.PlayOneShot(explosionSound);
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, 1.0f); // 簡易的な再生 (音量1.0f)
        }

        // 範囲内のコライダーを取得
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in colliders)
        {
            // ターゲット破壊処理
            PooledTarget target = hit.GetComponent<PooledTarget>();
            if (target != null)
            {
                // Debug.Log("爆発によりターゲット " + hit.name + " を破壊します。");
                TargetPoolManager.Instance.ReturnTarget(target); // ターゲットをプールに戻す
            }

            // (オプション) 他のRigidbodyに爆風の力を加える
            // Rigidbody rb = hit.GetComponent<Rigidbody>();
            // if (rb != null)
            // {
            //     rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            // }
        }

        // グレネード自身を破棄
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        // イベント購読解除
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(HandleSelectEntered);
            grabInteractable.selectExited.RemoveListener(HandleSelectExited);
        }

        // GrenadeManagerから自身を登録解除
        if (GrenadeManager.Instance != null)
        {
            GrenadeManager.Instance.UnregisterGrenade();
        }

        // ★OnDestroyでoriginalSocketがまだ残っている場合の処理を追加
        if (originalSocket != null && !wasPickedFromSocket) // まだソケットから「持ち去られた」と処理されていない場合
        {
            // これは、グレネードがソケット内で、プレイヤーに一度も掴まれず、
            // かつ物理的にも離脱せずに（例えば爆発に巻き込まれるなどで）直接Destroyされたケースを想定
            originalSocket.OnGrenadeTaken(); // ソケットを確実に空き状態にする
            Debug.Log($"Grenade {gameObject.name} destroyed while still in socket {originalSocket.gameObject.name}. Forcing socket to empty.");
        }

        // 全てのコルーチンを停止
        if (timedDespawnCoroutine != null) StopCoroutine(timedDespawnCoroutine);
        if (delayedKinematicChangeCoroutine != null) StopCoroutine(delayedKinematicChangeCoroutine);
        if (dislodgementCheckCoroutine != null) StopCoroutine(dislodgementCheckCoroutine);
    }

    // (デバッグ用) 爆発範囲をシーンビューに表示
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
        if (proximityTrigger != null && proximityTrigger.enabled)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, proximityTrigger.radius);
        }
    }
}