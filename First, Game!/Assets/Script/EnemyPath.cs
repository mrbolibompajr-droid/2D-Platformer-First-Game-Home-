using UnityEngine;

public class EnemyPath : MonoBehaviour
{
    public GameObject PointA;
    public GameObject PointB;//referar till b�da punkterna i unity
    private Rigidbody2D rb;
    private Animator anim;
    private Transform currentPoint;
    public float speed;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        currentPoint = PointB.transform;// s� man har en start punkt
        anim.SetBool("IsRunning", true);
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 point = currentPoint.position - transform.position;// ger vilke direction enemyn vill g� vilket �r mot currentPoint



        if (currentPoint == PointB.transform)
        {
            rb.linearVelocity = new Vector2(speed, 0);// om punkten �r punkt B g� mot punkte A
        }
        else if (currentPoint == PointA.transform)
        {
            rb.linearVelocity = new Vector2(-speed, 0);// om punkten �r punkt A g� mot punkte B
        }




        if (Vector2.Distance(transform.position, currentPoint.position) < 4f && currentPoint == PointA.transform)//om enmyn har n�tt currentpoint och den �r B ska currnet point s�ttas till punktA
        {
            print("byt till B");
            flip();
            currentPoint = PointB.transform;
        }

        else if (Vector2.Distance(transform.position, currentPoint.position) < 4f && currentPoint == PointB.transform)//om enmyn har n�tt currentpoint och den �r B ska currnet point s�ttas till punktA
        {
            print("byt till A");
            flip();
            currentPoint = PointA.transform;
        }
    }
    private void flip()
    {
        Vector3 localscale = transform.localScale;
        localscale.x *= -1;
        transform.localScale = localscale; // g�r s� den flipar
    }


    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(PointA.transform.position, 0.5f);
        Gizmos.DrawWireSphere(PointB.transform.position, 0.5f);
        Gizmos.DrawLine(PointA.transform.position, PointB.transform.position);
        // g�r punkterna tydligare

    }
}
