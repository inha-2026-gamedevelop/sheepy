// Unity
using UnityEngine;

[RequireComponent(typeof(HingeJoint2D))]
public class RandomPushPendulum : MonoBehaviour
{
    /****************************************
    *                Fields
    ****************************************/
    [Header("무작위 힘의 세기")]
    public float minForce = 0.2f;
    public float maxForce = 0.8f;

    //[Header("무작위 질량(Mass) 범위")]
    //public float minMass = 0.5f;
    //public float maxMass = 1.5f;

    //[Header("무작위 중력(Gravity Scale) 범위")]
    //public float minGravity = 0.8f;
    //public float maxGravity = 1.5f;


    /****************************************
    *              Unity Event
    ****************************************/
    void Start()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            // 1. 각 감쇠를 0으로 설정
            rb.angularDamping = 0.0f;

            // 2. 질량(Mass)을 범위 내에서 랜덤하게 설정
            //float randomMass = Random.Range(minMass, maxMass);
            //rb.mass = randomMass;

            // 3. 중력 계수(Gravity Scale)를 범위 내에서 랜덤하게 설정
            //float randomGravity = Random.Range(minGravity, maxGravity);
            //rb.gravityScale = randomGravity;

            // 4. 왼쪽(-1) 또는 오른쪽(1) 방향을 무작위로 결정
            float randomDirection = Random.Range(0, 2) == 0 ? -1f : 1f;

            // 5. 무작위 힘의 세기를 결정
            float randomForce = Random.Range(minForce, maxForce);

            // 6. 해당 방향으로 힘을 가합니다.
            rb.AddForce(new Vector2(randomDirection * randomForce, 0f), ForceMode2D.Impulse);
        }
    }
}
