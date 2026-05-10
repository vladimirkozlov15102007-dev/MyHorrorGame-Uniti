using UnityEngine;
using OldAmberFactory.Damage;

namespace OldAmberFactory.Weapons
{
    /// <summary>
    /// Abstract firearm. Derive for pistol, rifle, etc.
    /// </summary>
    public abstract class WeaponBase : MonoBehaviour
    {
        [Header("Ballistics")]
        [SerializeField] protected float baseDamage = 10f;    // head=30, torso=20, legs=10
        [SerializeField] protected float fireRate = 4.5f;      // shots per second
        [SerializeField] protected float range = 80f;
        [SerializeField] protected float spread = 0.012f;
        [SerializeField] protected float armorPenetration = 0.35f;

        [Header("Ammo")]
        [SerializeField] protected int magazineSize = 12;
        [SerializeField] protected int reserveAmmo = 60;
        [SerializeField] protected float reloadTime = 1.8f;

        [Header("Feel")]
        [SerializeField] protected float verticalRecoil = 1.2f;
        [SerializeField] protected float horizontalRecoil = 0.4f;
        [SerializeField] protected float recoilRecovery = 8f;
        [SerializeField] protected float adsFov = 48f;
        [SerializeField] protected float hipFov = 68f;
        [SerializeField] protected float adsSpeed = 9f;

        [Header("References")]
        [SerializeField] protected Transform muzzle;
        [SerializeField] protected Transform ejectPort;
        [SerializeField] protected ParticleSystem muzzleFlash;
        [SerializeField] protected GameObject shellPrefab;
        [SerializeField] protected LineRenderer tracerPrefab;
        [SerializeField] protected GameObject impactPrefab;
        [SerializeField] protected AudioSource audioSource;
        [SerializeField] protected AudioClip fireClip;
        [SerializeField] protected AudioClip reloadClip;
        [SerializeField] protected AudioClip emptyClip;
        [SerializeField] protected Animator animator;

        [Header("Camera / Sway")]
        [SerializeField] protected Transform weaponRoot;
        [SerializeField] protected float swayAmount = 0.02f;
        [SerializeField] protected float swaySmooth = 6f;
        [SerializeField] protected float walkBobAmplitude = 0.018f;
        [SerializeField] protected float walkBobFrequency = 7f;

        public int CurrentMag { get; protected set; }
        public int Reserve { get; protected set; }
        public bool IsReloading { get; protected set; }
        public bool IsAiming { get; protected set; }
        public float Heat { get; protected set; } // 0..1

        protected float _nextShot;
        protected Vector3 _recoilOffset;
        protected Vector3 _swayOffset;
        protected Vector3 _defaultLocalPos;
        protected float _bobTimer;

        public event System.Action<int, int> OnAmmoChanged;

        protected virtual void Awake()
        {
            CurrentMag = magazineSize;
            Reserve = reserveAmmo;
            if (weaponRoot) _defaultLocalPos = weaponRoot.localPosition;
        }

        public bool TryFire(Camera cam, GameObject instigator)
        {
            if (IsReloading) return false;
            if (Time.time < _nextShot) return false;
            if (CurrentMag <= 0)
            {
                if (emptyClip && audioSource) audioSource.PlayOneShot(emptyClip, 0.6f);
                _nextShot = Time.time + 0.25f;
                return false;
            }
            _nextShot = Time.time + 1f / Mathf.Max(0.1f, fireRate);
            DoShot(cam, instigator);
            ApplyRecoil();
            EmitFX();
            BroadcastNoise();
            CurrentMag--;
            OnAmmoChanged?.Invoke(CurrentMag, Reserve);
            return true;
        }

        protected virtual void DoShot(Camera cam, GameObject instigator)
        {
            if (cam == null) return;
            Vector3 origin = cam.transform.position;
            Vector3 dir = cam.transform.forward;
            dir += (Vector3)(Random.insideUnitCircle * spread);
            dir.Normalize();

            if (Physics.Raycast(origin, dir, out var hit, range, ~0, QueryTriggerInteraction.Ignore))
            {
                if (tracerPrefab && muzzle) SpawnTracer(muzzle.position, hit.point);

                var hitbox = hit.collider.GetComponent<Hitbox>();
                if (hitbox == null) hitbox = hit.collider.GetComponentInParent<Hitbox>();
                var info = new DamageInfo
                {
                    baseDamage = baseDamage,
                    hitPoint = hit.point,
                    hitDirection = dir,
                    hitForce = dir * 800f,
                    source = instigator,
                    isProjectile = true,
                    armorPenetration = armorPenetration
                };
                if (hitbox != null) hitbox.Receive(info);
                else
                {
                    // Generic environment impact.
                    if (impactPrefab)
                    {
                        var rot = Quaternion.LookRotation(hit.normal);
                        var go = Instantiate(impactPrefab, hit.point, rot);
                        Destroy(go, 5f);
                    }
                    if (hit.rigidbody) hit.rigidbody.AddForceAtPosition(info.hitForce, hit.point);
                }
            }
            else if (tracerPrefab && muzzle)
            {
                SpawnTracer(muzzle.position, origin + dir * range);
            }
        }

        protected void SpawnTracer(Vector3 a, Vector3 b)
        {
            var tr = Instantiate(tracerPrefab);
            tr.positionCount = 2;
            tr.SetPosition(0, a);
            tr.SetPosition(1, b);
            Destroy(tr.gameObject, 0.06f);
        }

        protected void ApplyRecoil()
        {
            _recoilOffset += new Vector3(
                -verticalRecoil,
                Random.Range(-horizontalRecoil, horizontalRecoil),
                0f);
        }

        protected virtual void EmitFX()
        {
            if (muzzleFlash) muzzleFlash.Play();
            if (fireClip && audioSource) audioSource.PlayOneShot(fireClip, 1f);
            if (shellPrefab && ejectPort)
            {
                var shell = Instantiate(shellPrefab, ejectPort.position, ejectPort.rotation);
                if (shell.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.AddForce(ejectPort.right * Random.Range(1.5f, 2.3f) + ejectPort.up * Random.Range(0.5f, 0.9f), ForceMode.Impulse);
                    rb.AddTorque(Random.insideUnitSphere * 4f, ForceMode.Impulse);
                }
                Destroy(shell, 6f);
            }
            if (animator) animator.SetTrigger("Fire");
        }

        protected void BroadcastNoise()
        {
            Core.EventBus.Publish(new Core.EventBus.NoiseEvent
            {
                position = transform.position,
                radius = 40f,
                intensity = 1f,
                source = gameObject
            });
        }

        public void StartReload()
        {
            if (IsReloading || CurrentMag == magazineSize || Reserve <= 0) return;
            IsReloading = true;
            if (animator) animator.SetTrigger("Reload");
            if (reloadClip && audioSource) audioSource.PlayOneShot(reloadClip, 0.8f);
            Invoke(nameof(FinishReload), reloadTime);
        }

        void FinishReload()
        {
            int needed = magazineSize - CurrentMag;
            int taken = Mathf.Min(needed, Reserve);
            CurrentMag += taken;
            Reserve -= taken;
            IsReloading = false;
            OnAmmoChanged?.Invoke(CurrentMag, Reserve);
        }

        public void SetAiming(bool value) { IsAiming = value; }

        protected virtual void LateUpdate()
        {
            if (weaponRoot == null) return;

            float mx = Input.GetAxis("Mouse X");
            float my = Input.GetAxis("Mouse Y");
            Vector3 targetSway = new Vector3(-mx * swayAmount, -my * swayAmount, 0f);
            _swayOffset = Vector3.Lerp(_swayOffset, targetSway, Time.deltaTime * swaySmooth);

            // Bob when moving.
            var cc = GetComponentInParent<CharacterController>();
            float speed = cc ? new Vector3(cc.velocity.x, 0f, cc.velocity.z).magnitude : 0f;
            _bobTimer += Time.deltaTime * walkBobFrequency * Mathf.Clamp01(speed);
            Vector3 bob = new Vector3(
                Mathf.Sin(_bobTimer * 0.5f) * walkBobAmplitude,
                Mathf.Abs(Mathf.Sin(_bobTimer)) * walkBobAmplitude,
                0f) * Mathf.Clamp01(speed / 6f);

            // Recoil decay.
            _recoilOffset = Vector3.Lerp(_recoilOffset, Vector3.zero, Time.deltaTime * recoilRecovery);

            weaponRoot.localPosition = _defaultLocalPos + _swayOffset + bob + _recoilOffset * 0.01f;
            weaponRoot.localRotation = Quaternion.Euler(_recoilOffset);
        }
    }
}
