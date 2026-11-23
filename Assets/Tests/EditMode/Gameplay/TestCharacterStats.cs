/// <summary>CharacterStats with a death counter, used to check that Die runs only once.</summary>
public class TestCharacterStats : CharacterStats
{
    public int Deaths { get; private set; }

    public void Setup(int maxHealthValue)
    {
        maxHealth = maxHealthValue;
        health = maxHealthValue;
        isDead = false;
    }

    public override void Die()
    {
        base.Die();
        Deaths++;
    }
}
