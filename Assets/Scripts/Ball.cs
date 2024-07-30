using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    [SerializeField, Min(0f)]
    float maxXSpeed = 20f,
        maxStartXSpeed = 2f,
        startXSpeed = 8f,
        constantYSpeed = 10f,
        extents = 0.5f;

    Vector2 position,
        velocity;

    public float Extents => extents;

    public Vector2 Position => position;

    public Vector2 Velocity => velocity;

    public void UpdateVisualization() =>
        transform.localPosition = new Vector3(position.x, 0f, position.y);

    public void Move() => position += velocity * Time.deltaTime;

    void Awake() => gameObject.SetActive(false);

    public void StartNewGame()
    {
        position = Vector2.zero;
        UpdateVisualization();
        velocity.x = Random.Range(-maxStartXSpeed, maxStartXSpeed);
        velocity.y = -constantYSpeed;
        gameObject.SetActive(true);
    }

    public void EndGame()
    {
        position.x = 0f;
        gameObject.SetActive(false);
    }

    public void SetXPositionAndSpeed(float start, float speedFactor, float deltaTime)
    {
        velocity.x = maxXSpeed * speedFactor;
        position.x = start + velocity.x * deltaTime;
    }

    /*
    A proper bounce happens when the ball's edge touches a boundary, not its center.
    So we need to know the ball's size, for which we'll add a configuration field expressed as extents,
    set to 0.5 by default, matching the unit cube.
    */
    public void BounceX(float boundary)
    {
        position.x = 2f * boundary - position.x;
        velocity.x = -velocity.x;
    }

    public void BounceY(float boundary)
    {
        position.y = 2f * boundary - position.y;
        velocity.y = -velocity.y;
    }
}
