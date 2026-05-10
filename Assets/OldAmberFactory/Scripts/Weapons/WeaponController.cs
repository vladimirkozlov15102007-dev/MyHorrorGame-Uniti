using UnityEngine;
using OldAmberFactory.Throwables;

namespace OldAmberFactory.Weapons
{
    /// <summary>
    /// Player's top-level weapon logic. Switches between WeaponBase and throwable mode.
    /// LMB fires, RMB aims, R reloads. When holding a Throwable, LMB charges throw power.
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private WeaponBase equippedWeapon;
        [SerializeField] private ThrowableHolder throwables;
        [SerializeField] private float adsFovSmooth = 9f;
        [SerializeField] private float hipFov = 68f;
        [SerializeField] private float adsFov = 48f;

        public WeaponBase Weapon => equippedWeapon;

        void Update()
        {
            if (throwables && throwables.HasThrowable)
            {
                HandleThrowable();
                return;
            }

            if (equippedWeapon == null) return;

            bool aiming = Input.GetMouseButton(1);
            equippedWeapon.SetAiming(aiming);
            if (playerCamera)
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView,
                    aiming ? adsFov : hipFov, Time.deltaTime * adsFovSmooth);

            if (Input.GetMouseButton(0))
                equippedWeapon.TryFire(playerCamera, gameObject);

            if (Input.GetKeyDown(KeyCode.R))
                equippedWeapon.StartReload();
        }

        void HandleThrowable()
        {
            if (playerCamera)
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, hipFov, Time.deltaTime * adsFovSmooth);

            if (Input.GetMouseButton(0)) throwables.Charge(Time.deltaTime);
            if (Input.GetMouseButtonUp(0)) throwables.Release();
        }
    }
}
