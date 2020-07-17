﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Experimental.PlayerLoop;
using System;

public class SimpleCarController : MonoBehaviour
{
    [Header("Input")]
    public string verticalAxisName;
    public string horizontalAxisName;
    public string brakeButton;
    
    private float verticleInput;
    private float hoziontalInput;

    private bool brakesApplied = false;

    public Vehicle car;

    void Start()
    {
        car.SetCenterOfMass();
    }

    private void Update()
    {
        // Get driving input
        verticleInput = Input.GetAxis(verticalAxisName);
        hoziontalInput = Input.GetAxis(horizontalAxisName);

        // Check if brakes are being applied
        brakesApplied = Input.GetButton(brakeButton);
    }

    void FixedUpdate()
    {
        // TODO: Remove debug print
        print("Vw: " + GetWheelVelocity().ToString("f0") + "km/h   Vc: " + GetVehicleVelocity().ToString("f0") + "km/h   RPM: " + GetWheelRpm().ToString("f0"));

        float motorTorque;
        float steeringTorque;

        AdjustSteering();

        GetDrivingInput(out motorTorque, out steeringTorque);

        ControlRpm(ref motorTorque);

        Stabilize();

        ApplyTorque(motorTorque, steeringTorque);

        if (brakesApplied)
        {
            ApplyBrakes();
        }
        else
        {
            ReleaseBrakes();
        }
    }

    // Properties

    // Get the maximum speed of the vehicle
    public float MaxSpeed
    {
        get
        {
            WheelCollider wheel = axleInfos[0].rightWheel;
            return wheel.radius * Mathf.PI * maxRPM * 60.0f / 1000.0f;
        }
    }

    // Methods

    // Finds the velocity of the vehicle based on its position & previous position
    float GetVehicleVelocity()
    {
        Vector3 velocityVector = rb.velocity;
        float velocity = velocityVector.magnitude;

        // Check sign of the velocity
        if (Vector3.Dot(Vector3.forward, velocityVector) < 0)
        {
            velocity = -velocity;
        }

        previousPosition = transform.position;
        return velocity;
    }

    // Returns the wheel velocity in km/h
    public float GetWheelVelocity()
    {
        WheelCollider wheel = axleInfos[0].rightWheel;
        return wheel.radius * Mathf.PI * wheel.rpm * 60.0f / 1000.0f;
    }

    // Returns the rotations per minute of the wheels
    public float GetWheelRpm()
    {
        return axleInfos [0].rightWheel.rpm;
    }

    // Linearly interpolates the steering angle from max to min based on speed
    void AdjustSteering()
    {
        steeringAngle = Mathf.Lerp(maxSteeringAngle, minSteeringAngle, GetWheelVelocity() / MaxSpeed);
    }

    // Get driving input
    void GetDrivingInput(out float motorTorque, out float steeringTorque)
    {
        motorTorque = maxMotorTorque * verticleInput;
        steeringTorque = steeringAngle * hoziontalInput;
    }

    void ControlRpm(ref float motorTorque)
    {
        if (GetWheelRpm() < idealRPM)
        {
            // Replace MAGIC number 10.0f
            motorTorque = Mathf.Lerp(motorTorque / 10.0f, motorTorque, GetWheelRpm() / idealRPM);
        }
        else
        {
            // Apply max torque at ideal rpm and zero torque at max rpm
            motorTorque = Mathf.Lerp(motorTorque, 0.0f, (GetWheelRpm() - idealRPM) / (maxRPM - idealRPM));
        }
    }

    // Apply force on the wheels to prevent tipping
    void Stabilize()
    {
        foreach (AxleInfo axleInfo in axleInfos)
        {
            WheelCollider leftWheel = axleInfo.leftWheel;
            WheelCollider rightWheel = axleInfo.rightWheel;
            WheelHit wheelHit;
            float leftTravel = 1.0f;
            float rightTravel = 1.0f;

            bool isGroundedLeft = leftWheel.GetGroundHit(out wheelHit);
            if (isGroundedLeft)
            {
                leftTravel = (-leftWheel.transform.InverseTransformPoint(wheelHit.point).y - leftWheel.radius) / leftWheel.suspensionDistance;
            }

            bool isGroundedRight = rightWheel.GetGroundHit(out wheelHit);
            if (isGroundedRight)
            {
                rightTravel = (-rightWheel.transform.InverseTransformPoint(wheelHit.point).y - rightWheel.radius) / rightWheel.suspensionDistance;
            }

            float antiRollForce = (leftTravel - rightTravel) * antiRollStrength;

            if (isGroundedLeft)
            {
                rb.AddForceAtPosition(leftWheel.transform.up * -antiRollForce, leftWheel.transform.position);
            }
            if (isGroundedRight)
            {
                rb.AddForceAtPosition(rightWheel.transform.up * antiRollForce, rightWheel.transform.position);
            }
        }
    }

    // Apply torque to each wheel
    void ApplyTorque(float motor, float steering)
    {
        foreach (AxleInfo axleInfo in axleInfos)
        {
            if (axleInfo.steering)
            {
                axleInfo.leftWheel.steerAngle = steering;
                axleInfo.rightWheel.steerAngle = steering;
            }
            if (axleInfo.motor)
            {
                axleInfo.leftWheel.motorTorque = motor;
                axleInfo.rightWheel.motorTorque = motor;
            }
            ApplyLocalPositionToVisuals(axleInfo.leftWheel);
            ApplyLocalPositionToVisuals(axleInfo.rightWheel);
        }
    }

    // Apply transforms to corresponding visual wheels
    public void ApplyLocalPositionToVisuals(WheelCollider collider)
    {
        if (collider.transform.childCount == 0)
        {
            return;
        }

        Transform visualWheel = collider.transform.GetChild(0);

        Vector3 position;
        Quaternion rotation;
        collider.GetWorldPose(out position, out rotation);

        visualWheel.transform.position = position;
        visualWheel.transform.rotation = rotation;
    }

    // Applies brake torque to each wheel and applies a brake force to the car
    void ApplyBrakes()
    {
        bool isGrounded = false;

        foreach (AxleInfo axleInfo in axleInfos)
        {
            axleInfo.leftWheel.brakeTorque = brakeTorque;
            axleInfo.rightWheel.brakeTorque = brakeTorque;

            // Check if any of the wheels are on the ground
            if (axleInfo.leftWheel.isGrounded || axleInfo.rightWheel.isGrounded)
            {
                isGrounded = true;
            }
        }

        // Check if the vehicle is grounded
        if (isGrounded)
        {
            // Apply force in opposite direction of travel
            rb.AddForce(rb.velocity.normalized * -brakeForce);
        }
    }

    // Sets brake torque to zero
    void ReleaseBrakes()
    {
        foreach (AxleInfo axleInfo in axleInfos)
        {
            axleInfo.leftWheel.brakeTorque = 0.0f;
            axleInfo.rightWheel.brakeTorque = 0.0f;
        }
    }

    // Debug

    // Draw center of mass gizmo
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position + transform.rotation * comOffset, 0.4f);
    }
}