using UnityEngine;

public class MonsterController : MonoBehaviour
{
    [SerializeField] private float move_speed = 5.0f;
    [SerializeField] private int max_Hp = 10000;
    [SerializeField] private int current_Hp = 10000;

    private bool is_Dead = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TakeDamage(int damage)
    {
        if(current_Hp > 0)
        {
            if (!is_Dead)
            {
                current_Hp -= damage;
                Debug.Log($"몬스터 피격됨 : {damage} \n {current_Hp}/{max_Hp}");

                //때리는중에 사망시 처리
                if (current_Hp <= 0)
                {
                    IsDead();
                }
            }
        }
        else
        {
            IsDead();
        }
    }

    private void IsDead()
    {
        is_Dead = true;
        //TODO:사망 애니메이션
        Destroy(gameObject);
    }
}
