using UnityEngine;

public class LivelyCamera : MonoBehaviour
{
    [SerializeField, Min(0f)]
    float springStrength = 100f,
        dampingStrength = 10f,
        jostleStrength = 40f,
        pushStrength = 1f,
        maxDeltaTime = 1f / 60f;

    Vector3 anchorPosition,
        velocity;

    void Awake() => anchorPosition = transform.localPosition;

    public void JostleY() => velocity.y += jostleStrength;

    public void PushXZ(Vector2 impulse)
    {
        velocity.x += pushStrength * impulse.x;
        velocity.z += pushStrength * impulse.y;
    }

    /*
    Our simply spring rules only behave well as long as the frame rate is high enough. It resist pushing and jostling, pulling the camera back to its anchor point,
    but can cause some overshoot and might wiggle a bit before it comes to rest. However, if the frame rate is too low the overshoot might end up exaggerating its momentum
     and it can go out of control, speeding up instead of slowing down. This problem can be demonstrated by forcing a very low frame rate, by adding Application.targetFrameRate = 5; in an Awake method.
    You have to set it back to zero later to remove the limit, as this setting is persistent.

    The problem doesn't occur when the frame rate is high enough. So we can avoid it by enforcing a small time delta.
    We could do this by using FixedUpdate to move the camera. However, because that enforces an exact time delta this will result in micro stutters
    as the camera might not get updated the same amount of times each frame,
    which is very obvious because it affects the motion of the entire view. Also, it limits the effective frame rate of the camera's motion.

    A simple solution is to enforce a maximum time delta, but not a minimum. Add a configurable maximum for this to LivelyCamera,
    set to one sixtieth of a second by default. Then move the code from LateUpdate to a new TimeStep method with the time delta as a parameter.
    Have LateUpdate invoke TimeStep with the max delta as many times as it fits in the current frame's delta, then once more with the remaining delta.
    */
    void LateUpdate()
    {
        float dt = Time.deltaTime;
        while (dt > maxDeltaTime)
        {
            TimeStep(maxDeltaTime);
            dt -= maxDeltaTime;
        }
        TimeStep(dt);
    }

    void TimeStep(float dt)
    {
        Vector3 displacement = anchorPosition - transform.localPosition;
        Vector3 acceleration = springStrength * displacement - dampingStrength * velocity;
        velocity += acceleration * dt;
        transform.localPosition += velocity * dt;
    }
}
