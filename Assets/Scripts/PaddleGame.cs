using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PaddleGame : MonoBehaviour
{
    [SerializeField, Min(2)]
    int pointsToWin = 3;

    [SerializeField]
    Ball ball;

    [SerializeField]
    Paddle bottomPaddle,
        topPaddle;

    [SerializeField, Min(0f)]
    Vector2 arenaExtents = new Vector2(10f, 10f);

    [SerializeField]
    TextMeshPro countdownText;

    [SerializeField, Min(1f)]
    float newGameDelay = 3f;

    float countdownUntilNewGame;

    void Awake() => countdownUntilNewGame = newGameDelay;

    void StartNewGame()
    {
        ball.StartNewGame();
        bottomPaddle.StartNewGame();
        topPaddle.StartNewGame();
    }

    void Update()
    {
        bottomPaddle.Move(ball.Position.x, arenaExtents.x);
        topPaddle.Move(ball.Position.x, arenaExtents.x);

        if (countdownUntilNewGame <= 0f)
        {
            UpdateGame();
        }
        else
        {
            UpdateCountdown();
        }
    }

    void UpdateGame()
    {
        ball.Move();
        BounceYIfNeeded();
        BounceXIfNeeded(ball.Position.x);
        ball.UpdateVisualization();
    }

    void UpdateCountdown()
    {
        countdownUntilNewGame -= Time.deltaTime;
        if (countdownUntilNewGame <= 0f)
        {
            countdownText.gameObject.SetActive(false);
            StartNewGame();
        }
        else
        {
            float displayValue = Mathf.Ceil(countdownUntilNewGame);
            if (displayValue < newGameDelay)
            {
                countdownText.SetText("{0}", displayValue);
            }
        }
    }

    /*
    Is it possible for the ball to escape during a frame rate dip?
    Theoretically yes. If it moved so fast that it should bounce off opposite edges in the same dimension during a single time step, then it would escape for a single frame.
    However, Unity's default maximum time delta is a third of a second, so this requires a speed greater than 60 for our arena, which would be too fast to be playable.
    */
    void BounceYIfNeeded()
    {
        float yExtents = arenaExtents.y - ball.Extents;
        if (ball.Position.y < -yExtents)
        {
            BounceY(-yExtents, bottomPaddle, topPaddle);
        }
        else if (ball.Position.y > yExtents)
        {
            BounceY(yExtents, topPaddle, bottomPaddle);
        }
    }

    /*
    The first thing BounceY must do is determine how long ago the bounce happened. This is found by subtracting the boundary from the ball's Y position and dividing that by the ball's Y velocity.
    Note that we ignore that the paddle is a little thicker than the boundary, as that's just a visual thing to avoid Z fighting while rendering.

    Next, calculate the ball's X position when the bounce happened.

    After that we perform the original Y bounce, and then we check whether the defending paddle hit the ball.
    If so, set the ball's X position and speed, based on the bounce X position, the hit factor, and how long ago it happened.

    At this point we have to consider the possibility that a bounce happened in both dimensions.
    In that case the X position of the bounce might end up outside the arena. This can be prevented by performing the X bounce first, but only if needed.
    To support this change BounceXIfNeeded so the X position that it checks is provided via a parameter.

    Then we can also invoke BounceXIfNeeded in BounceY based on the where it would have hit the Y boundary. Thus we take care of an X bounce only if it happened before the Y bounce.
    After that once again calculate the bounce X position, now potentially based on a different ball position and velocity.

    Next, the ball's velocity changes depending on where it hit a paddle. Its Y speed always remains the same while its X speed is variable.
    This means that it always takes the same amount of time to move from paddle to paddle,
    but it might move sideways a little or a lot. Pong's ball behaves the same way.

    What's different from Pong is that in our game the ball still bounces off the arena's edge when a paddle misses it, while in Pong that triggers a new round.
    Our game just keeps going without interruption, not interrupting gameplay. Let's keep this behavior as a unique quirk of our game.
    */
    void BounceY(float boundary, Paddle defender, Paddle attacker)
    {
        float durationAfterBounce = (ball.Position.y - boundary) / ball.Velocity.y;
        float bounceX = ball.Position.x - ball.Velocity.x * durationAfterBounce;

        BounceXIfNeeded(bounceX);
        bounceX = ball.Position.x - ball.Velocity.x * durationAfterBounce;
        ball.BounceY(boundary);

        if (defender.HitBall(bounceX, ball.Extents, out float hitFactor)) // whoa output param as part of function input params
        {
            ball.SetXPositionAndSpeed(bounceX, hitFactor, durationAfterBounce);
        }
        else if (attacker.ScorePoint(pointsToWin))
        {
            EndGame();
        }
    }

    void EndGame()
    {
        countdownUntilNewGame = newGameDelay;
        countdownText.SetText("GAME OVER");
        countdownText.gameObject.SetActive(true);
        ball.EndGame();
    }

    void BounceXIfNeeded(float x)
    {
        float xExtents = arenaExtents.x - ball.Extents;
        if (x < -xExtents)
        {
            ball.BounceX(-xExtents);
        }
        else if (x > xExtents)
        {
            ball.BounceX(xExtents);
        }
    }
}
