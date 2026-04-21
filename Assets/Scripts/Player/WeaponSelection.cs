// Handoff statico tra il menu di selezione arma (task #4) e la GameScene.
// Il menu chiama SetWeapon() prima di caricare la scena; PlayerShooting lo legge in Awake().
public static class WeaponSelection
{
    public static WeaponData CurrentWeapon { get; private set; }
    public static void SetWeapon(WeaponData weapon) => CurrentWeapon = weapon;
    public static void Clear() => CurrentWeapon = null;
}
