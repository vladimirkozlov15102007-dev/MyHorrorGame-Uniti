using OldAmberFactory.Audio;
using OldAmberFactory.Damage;
using UnityEngine;

namespace OldAmberFactory.Weapons
{
    /// <summary>
    /// Hit-scan pistol. Applies recoil, spawns muzzle-flash VFX, ejects a shell,
    /// and broadcasts a gunshot noise to AI.
    /// Damage is resolved by the HitBox the ray hits: zone multipliers produce
    /// 30 head / 20 torso / 10 legs per design.
    /// </summary>
    public class Pistol : WeaponBase
    {
        [Header("Ballistics")]
        [SerializeField] private Transform _muzzle;
        [SerializeField] private Camera _aimCamera;
        [SerializeField] private float _maxRange = 120f;
        [SerializeField] private float _baseDamage = 20f;     // torso reference damage
        [SerializeField] private float _impulse = 7f;
        [SerializeField] private float _hipSpreadDeg = 2.2f;
        [SerializeField] private float _aimSpreadDeg = 0.3f;
        [SerializeField] private LayerMask _hitMask = ~0;

        [Header("Recoil")]
        [SerializeField] private Vector2 _recoilPitchRange = new(1.2f, 2.0f);
        [SerializeField] private Vector2 _recoilYawRange = new(-0.6f, 0.6f);
        [SerializeField] private float _recoilRecovery = 8f;

        [Header("FX hooks")]
        [SerializeField] private ParticleSystem _muzzleFlash;
        [SerializeField] private ParticleSystem _shellEject;
        [SerializeField] private AudioSource _fireSource;
        [SerializeField] private AudioClip _fireClip;
        [SerializeField] private GameObject _bulletImpactPrefab;

        [Header("Weapon animation")]
        [SerializeField] private Animator _animator;

        private Vector2 _recoilAccum;
        private Vector2 _recoilTarget;

        protected override void Update()
        {
            base.Update();
            // recoil decay
            _recoilAccum = Vector2.Lerp(_recoilAccum, Vector2.zero, _recoilRecovery * Time.deltaTime);
            _recoilTarget = Vector2.Lerp(_recoilTarget, _recoilAccum, 12f * Time.deltaTime);

            if (_aimCamera != null)
            {
                // apply recoil to camera pivot via its parent rotation additive — here we just nudge local euler
                var t = _aimCamera.transform;
                t.localRotation = Quaternion.Euler(-_recoilTarget.y, _recoilTarget.x, 0f);
            }
        }

        protected override void FireShot()
        {
            if (_aimCamera == null) return;

            float spread = IsAiming ? _aimSpreadDeg : _hipSpreadDeg;
            Vector3 dir = GetScatteredDirection(_aimCamera.transform.forward, spread);
            Vector3 origin = _aimCamera.transform.position;

            if (Physics.Raycast(origin, dir, out var hit, _maxRange, _hitMask, QueryTriggerInteraction.Ignore))
            {
                ResolveHit(hit, dir);
            }

            ApplyRecoil();
            PlayFireFX();
            Vector3 noisePos = _muzzle != null ? _muzzle.position : transform.position;
            NoiseEventBus.Broadcast(noisePos, 35f, NoiseSource.Gunshot, gameObject);
        }

        private void ResolveHit(RaycastHit hit, Vector3 dir)
        {
            var hb = hit.collider.GetComponent<HitBox>();
            if (hb != null)
            {
                // Per-design the body-part damage table is absolute (30/20/10).
                // Weapon's _baseDamage acts as a proportional multiplier: 20 = nominal.
                float weaponScale = _baseDamage / 20f;
                float dmg = DamageTable.ForBodyPart(hb.Part) * weaponScale;
                var info = DamageInfo.FromHit(dmg, hb.Part, hit, dir, _impulse, gameObject);
                hb.Receive(info);
            }
            else
            {
                // prop or static — just apply an impulse
                if (hit.rigidbody != null)
                    hit.rigidbody.AddForceAtPosition(dir * _impulse, hit.point, ForceMode.Impulse);
            }

            if (_bulletImpactPrefab != null)
            {
                Object.Instantiate(_bulletImpactPrefab, hit.point,
                    Quaternion.LookRotation(hit.normal));
            }
        }

        private Vector3 GetScatteredDirection(Vector3 forward, float degrees)
        {
            if (degrees <= 0.0001f) return forward;
            float yaw   = Random.Range(-degrees, degrees);
            float pitch = Random.Range(-degrees, degrees);
            return Quaternion.Euler(pitch, yaw, 0f) * forward;
        }

        private void ApplyRecoil()
        {
            float factor = IsAiming ? 0.55f : 1f;
            _recoilAccum += new Vector2(
                Random.Range(_recoilYawRange.x, _recoilYawRange.y) * factor,
                Random.Range(_recoilPitchRange.x, _recoilPitchRange.y) * factor
            );
        }

        private void PlayFireFX()
        {
            if (_muzzleFlash != null) _muzzleFlash.Play(true);
            if (_shellEject  != null) _shellEject.Play(true);
            if (_fireSource != null && _fireClip != null)
            {
                _fireSource.pitch = Random.Range(0.97f, 1.03f);
                _fireSource.PlayOneShot(_fireClip);
            }
            if (_animator != null) _animator.SetTrigger("Fire");
        }

        protected override void OnReloadStarted()
        {
            if (_animator != null) _animator.SetTrigger("Reload");
        }
    }
}
