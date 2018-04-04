using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AJE
{
    class PIDController
    {
        float Kp, Ki, Kd;
        float setPoint, actual, dt;
        float error, previousError;
        float intergral, derivative;
        float ceiling;
        public PIDController()
        {
            Kp = Ki = Kd = 0;
            this.setPoint = 0;
            this.actual = 0;
            this.dt = 1;
            this.intergral = 0;
            this.ceiling = 0;
        }

        public void Update(float kp, float ki, float kd, float setPoint, float actual, float dt, float ceiling)
        {
            this.Kp = kp;
            this.Ki = ki;
            this.Kd = kd;
            this.setPoint = setPoint;
            this.actual = actual;
            this.dt = dt;
        }
        public float getDrive()
        {
            error = setPoint - actual;
            intergral = Mathf.Clamp(intergral + error * dt, -ceiling, ceiling);
            derivative = (error - previousError) / dt;
            previousError = error;
            return error * Kp + intergral * Ki + derivative * Kd;
        }
    }
}
