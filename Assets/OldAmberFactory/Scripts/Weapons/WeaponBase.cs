using OldAmberFactory.Player;
using UnityEngine;

namespace OldAmberFactory.Weapons
{
    /// <summary>
    /// Shared behaviour for every equippable weapon: ammo accounting,
    /// reload timing, ADS state, fire rate gate. Concrete subclasses only implement FireShot().
    /// </summary>
    public abstract class WeaponBase : MonoBehaviour
    {
        [Header("Ammo")]
        [SerializeField] protected int _magazineSize = 12;
        [SerializeField] protected int _reserveAmmo = 48;
        [SerializeField] protected float _reloadTime = 1.8f;

        [Header("Fire")]
        [SerializeField] protected float _fireRate = 6f;         // shots per second
        [SerializeField] protected bool _fullAuto = false;

        [Header("ADS")]
        [SerializeField] protected Transform _hipTransform;
        [SerializeField] protected Transform _adsTransform;
        [SerializeField] protected float _adsLerp = 12f;

        [Header("Refs")]
        [SerializeField] protected InputRouter _input;
        [SerializeField] protected Transform _weaponRoot;

        protected int _currentMag;
        protected bool _reloading;
        protected float _reloadEnds;
        protected float _nextShotAt;

        public int Ammo => _currentMag;
        public int Reserve => _reserveAmmo;
        public int MagSize => _magazineSize;
        public bool IsReloading => _reloading;
        public bool IsAiming => _input != null && _input.Aim;

        protected virtual void Awake() => _currentMag = _magazineSize;

        protected virtual void Update()
        {
            if (_input == null) return;
            TickReload();
            TickADS();
            TickFire();
            if (_input.ReloadPressed) TryStartReload();
        }

        private void TickFire()
        {
            if (_reloading) return;
            if (Time.time < _nextShotAt) return;

            bool wantsFire = _fullAuto ? _input.FireHeld : _input.FirePressed;
            if (!wantsFire) return;

            if (_currentMag <= 0)
            {
                TryStartReload();
                return;
            }

            _nextShotAt = Time.time + 1f / Mathf.Max(0.01f, _fireRate);
            _currentMag--;
            FireShot();
        }

        protected abstract void FireShot();

        private void TickADS()
        {
            if (_weaponRoot == null || _hipTransform == null || _adsTransform == null) return;
            Transform target = IsAiming ? _adsTransform : _hipTransform;
            _weaponRoot.localPosition = Vector3.Lerp(
                _weaponRoot.localPosition, target.localPosition, _adsLerp * Time.deltaTime);
            _weaponRoot.localRotation = Quaternion.Slerp(
                _weaponRoot.localRotation, target.localRotation, _adsLerp * Time.deltaTime);
        }

        public void TryStartReload()
        {
            if (_reloading) return;
            if (_currentMag >= _magazineSize) return;
            if (_reserveAmmo <= 0) return;
            _reloading = true;
            _reloadEnds = Time.time + _reloadTime;
            OnReloadStarted();
        }

        private void TickReload()
        {
            if (!_reloading) return;
            if (Time.time < _reloadEnds) return;

            int needed = _magazineSize - _currentMag;
            int take = Mathf.Min(needed, _reserveAmmo);
            _currentMag += take;
            _reserveAmmo -= take;
            _reloading = false;
            OnReloadFinished();
        }

        protected virtual void OnReloadStarted() {}
        protected virtual void OnReloadFinished() {}
    }
}
