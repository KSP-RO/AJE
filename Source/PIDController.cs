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
        float integral, derivative;
        float[] e;
        int index, N;

        public PIDController(int N)
        {
            Kp = Ki = Kd = 0;
            this.setPoint = 0;
            this.actual = 0;
            this.dt = 1;
            this.integral = 0;
            this.N = N;
            e = new float[N];
            for (int i = 0; i < N; i++)
            {
                e[i] = 0;
            }
            index = 0;
            
        }

        public void Update(float kp, float ki, float kd, float setPoint, float actual, float dt)
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
            //integral = integral + error * dt;
            enqueue(error * dt);
            derivative = (error - previousError) / dt;
            previousError = error;
            return error * Kp + integral * Ki + derivative * Kd;
        }
        void enqueue(float x)
        {
            this.integral -= e[index];
            this.integral += x;
            e[index] = x;
            index++;
            if (index == N)
                index = 0;
            
        }
    }
}
