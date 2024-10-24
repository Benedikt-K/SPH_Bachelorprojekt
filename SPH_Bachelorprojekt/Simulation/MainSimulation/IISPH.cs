﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using SPH_Bachelorprojekt.Simulation.Particles;
using SPH_Bachelorprojekt.Simulation.Kernel_Function;

namespace SPH_Bachelorprojekt.Simulation.MainSimulation
{
    /// <summary>
    /// functions to compute the pressure using a global pressure computation
    /// </summary>

    class IISPH
    {
        public void PredictAdvection(List<Particle> Particles, float TimeStep, Kernel kernel)
        {
            Parallel.ForEach(Particles, particle =>
            {
                if (!particle.IsBoundaryParticle)
                {
                    particle.LastDensity = particle.Density;


                    Vector2 nonPressureAcceleration = SPH.CalculateViscosityAcceleration(particle, particle.Neighbours, kernel) + SPH.GetGravity(); ///add surface tension
                    particle.NonPressureForces = nonPressureAcceleration;
                    particle.PredictedVelocity = particle.Velocity + TimeStep * nonPressureAcceleration; //calculate predicted velocity
                    //Console.WriteLine("predictedVel: " + particle.PredictedVelocity + "nonP_Acc: " + nonPressureAcceleration);
                    particle.LastDensity = particle.Density;
                    particle.Density = density;
                }
            });



            Parallel.ForEach(Particles, particle =>
            {
                if (!particle.IsBoundaryParticle)

                {

                    // compute source term (predicted density Error)
                    float predictedDensityError = Density - particle.Density;
                    foreach (Particle neighbour in particle.Neighbours)
                    {
                        if (neighbour.IsBoundaryParticle)
                        {
                            //boundary particles
                            float dotProduct = Vector2.Dot(particle.PredictedVelocity - neighbour.PredictedVelocity * (ElapsedTime + TimeStep), kernel.GradW(particle.Position, neighbour.Position));
                            predictedDensityError -= TimeStep * neighbour.Mass * dotProduct;
                        }
                        else
                        {
                            /// FLUID particles
                            float dotProduct = Vector2.Dot(particle.PredictedVelocity - neighbour.PredictedVelocity, kernel.GradW(particle.Position, neighbour.Position));
                            predictedDensityError -= TimeStep * neighbour.Mass * dotProduct;
                        }
                    }
                    particle.SourceTerm = predictedDensityError;

                    // calculate diagonal element 
                    float diagonalTerm = 0;
                    foreach (Particle neighbour in particle.Neighbours)
                    {
                        float particleLastDensity2 = particle.LastDensity * particle.LastDensity;
                        if (neighbour.IsBoundaryParticle)
                        {
                            // BOUNDARY N
                            Vector2 innerTerm = Vector2.Zero;
                            foreach (Particle neighbourInner in particle.Neighbours)
                            {
                                if (neighbourInner.IsBoundaryParticle)
                                {
                                    // boundary NN
                                    innerTerm -= 2 * Gamma * neighbourInner.Mass / particleLastDensity2 * kernel.GradW(particle.Position, neighbourInner.Position);
                                }
                                else
                                {
                                    // fluid NN
                                    innerTerm -= neighbourInner.Mass / particleLastDensity2 * kernel.GradW(particle.Position, neighbourInner.Position);
                                }
                            }
                            float dotProduct = Vector2.Dot(innerTerm, kernel.GradW(particle.Position, neighbour.Position));
                            diagonalTerm += neighbour.Mass * dotProduct;
                        }
                        else
                        {
                            // FLUID N
                            Vector2 innerTerm = Vector2.Zero;
                            foreach (Particle neighbourInner in particle.Neighbours)
                            {
                                if (neighbourInner.IsBoundaryParticle)
                                {
                                    // boundary NN
                                    innerTerm -= 2 * Gamma * neighbourInner.Mass / particleLastDensity2 * kernel.GradW(particle.Position, neighbourInner.Position);
                                }
                                else
                                {
                                    // fluid NN
                                    innerTerm -= neighbourInner.Mass / particleLastDensity2 * kernel.GradW(particle.Position, neighbourInner.Position);
                                }
                            }
                            float dotProduct = Vector2.Dot(innerTerm, kernel.GradW(particle.Position, neighbour.Position));
                            diagonalTerm += neighbour.Mass * dotProduct;

                            //second fluid N term
                            Vector2 otherInnerTerm = Vector2.Zero;
                            otherInnerTerm = (particle.Mass / particleLastDensity2) * kernel.GradW(neighbour.Position, particle.Position);
                            float otherdotProduct = Vector2.Dot(otherInnerTerm, kernel.GradW(particle.Position, neighbour.Position));
                            diagonalTerm += neighbour.Mass * otherdotProduct;
                        }
                    }

                    diagonalTerm *= TimeStep * TimeStep;
                    particle.DiagonalElement = GetDiagonalElement();
                    particle.PredictedPressure = 0;
                }
            });
        }

        public float GetDiagonalElement(Particle particle, float particleSizeH, float TimeStep, float Gamma, Kernel kernel)
        {
            float diagonalElement = 0;
            foreach (Particle neighbour in particle.Neighbours)
            {
                float particleLastDensity2 = particle.LastDensity * particle.LastDensity;
                if (neighbour.IsBoundaryParticle)
                {
                    // BOUNDARY N
                    Vector2 innerTerm = Vector2.Zero;
                    foreach (Particle neighbourInner in particle.Neighbours)
                    {
                        if (neighbourInner.IsBoundaryParticle)
                        {
                            // boundary NN
                            innerTerm -= 2 * Gamma * neighbourInner.Mass / particleLastDensity2 * kernel.GradW(particle.Position, neighbourInner.Position);
                        }
                        else
                        {
                            // fluid NN
                            innerTerm -= neighbourInner.Mass / particleLastDensity2 * kernel.GradW(particle.Position, neighbourInner.Position);
                        }
                    }
                    float dotProduct = Vector2.Dot(innerTerm, kernel.GradW(particle.Position, neighbour.Position));
                    diagonalElement += neighbour.Mass * dotProduct;
                }
                else
                {
                    // FLUID N
                    Vector2 innerTerm = Vector2.Zero;
                    foreach (Particle neighbourInner in particle.Neighbours)
                    {
                        if (neighbourInner.IsBoundaryParticle)
                        {
                            // boundary NN
                            innerTerm -= 2 * Gamma * neighbourInner.Mass / particleLastDensity2 * kernel.GradW(particle.Position, neighbourInner.Position);
                        }
                        else
                        {
                            // fluid NN
                            innerTerm -= neighbourInner.Mass / particleLastDensity2 * kernel.GradW(particle.Position, neighbourInner.Position);
                        }
                    }
                    float dotProduct = Vector2.Dot(innerTerm, kernel.GradW(particle.Position, neighbour.Position));
                    diagonalElement += neighbour.Mass * dotProduct;

                    //second fluid N term
                    Vector2 otherInnerTerm = Vector2.Zero;
                    otherInnerTerm = (particle.Mass / particleLastDensity2) * kernel.GradW(neighbour.Position, particle.Position);
                    float otherdotProduct = Vector2.Dot(otherInnerTerm, kernel.GradW(particle.Position, neighbour.Position));
                    diagonalElement += neighbour.Mass * otherdotProduct;
                }
            }
            diagonalElement *= TimeStep * TimeStep;
            return diagonalElement;
        }


        public float GetSourceTerm(Particle particle, float particleSizeH, float TimeStep, float ElapsedTime, float fluidDensity, Kernel kernel)
        {
            /////////////// mybe here error ////////// check again
            float predictedDensityError = fluidDensity - particle.Density; // maybe LastDensity
            foreach (Particle neighbour in particle.Neighbours)
            {
                if (neighbour.IsBoundaryParticle)
                {
                    //boundary particles
                    float dotProduct = Vector2.Dot(particle.PredictedVelocity - neighbour.PredictedVelocity * (ElapsedTime + TimeStep), kernel.GradW(particle.Position, neighbour.Position));
                    predictedDensityError -= TimeStep * neighbour.Mass * dotProduct;
                }
                else
                {
                    /// FLUID particles
                    float dotProduct = Vector2.Dot(particle.PredictedVelocity - neighbour.PredictedVelocity, kernel.GradW(particle.Position, neighbour.Position));
                    predictedDensityError += TimeStep * neighbour.Mass * dotProduct;
                }
            }
            return predictedDensityError;
        }

        public float GetDivergence(Particle particle, float timeStep, Kernel kernel)
        {
            float timeStep2 = timeStep * timeStep;
            float Ap = 0f;
            // compute divergence of velocity change due to pressureAcc
            foreach (Particle neighbour in particle.Neighbours) // OLD = foreach (Particle neighbour in Particles)
            {
                if (neighbour.IsBoundaryParticle)
                {
                    float dotProduct = Vector2.Dot(particle.PressureAcceleration, kernel.GradW(particle.Position, neighbour.Position));
                    Ap += neighbour.Mass * dotProduct;
                }
                else
                {
                    float dotProduct = Vector2.Dot(particle.PressureAcceleration - neighbour.PressureAcceleration, kernel.GradW(particle.Position, neighbour.Position));
                    Ap += neighbour.Mass * dotProduct;
                }
            }
            Ap *= timeStep2;
            return Ap;
        }

        public void UpdateParticle(ref List<Particle> Particles, float TimeStep, Kernel kernel)
        {
            ///
            /// update particle positions and velocitys
            /// 
            foreach (Particle particle in Particles)
            {
                if (!particle.IsBoundaryParticle)
                {
                    particle.Velocity = TimeStep * particle.PressureAcceleration + particle.PredictedVelocity;
                    particle.Position += TimeStep * particle.Velocity;
                    particle.Pressure = particle.PredictedPressure;
                }
            }
        }

        public void PressureSolve(ref List<Particle> Particles, float Density, float Gamma, Kernel kernel)
        {
            ///
            /// calculate Pressures of all particles
            ///
            // dislocate to other file
            int min_Iterations = 20;
            int max_Iterations = 100;
            float max_error_Percentage = 1f; // given in %
            // dislocate to other file
            int currentIteration = 0;
            float averageDensityError = float.PositiveInfinity;
            bool continueWhile = true;
            float percentageDensityError = float.PositiveInfinity;


            while ((continueWhile || (currentIteration <= min_Iterations)) && (currentIteration < max_Iterations))
            {
                currentIteration++;
                averageDensityError = 0;
                DoPressureSolveIteration(ref Particles, Gamma, kernel, ref averageDensityError);
                percentageDensityError = averageDensityError / Density;
                float eta = max_error_Percentage * 0.01f * Density;
                continueWhile = averageDensityError >= eta;
                Console.WriteLine("iter: " + currentIteration + ", err: " + percentageDensityError);

            }
            Console.WriteLine("iterations needed: " + currentIteration);
        }

        public void DoPressureSolveIteration(ref List<Particle> Particles, float Gamma, Kernel kernel, ref float averageDensityError)
        {
            foreach (Particle particle in Particles)
            {
                if (!particle.IsBoundaryParticle)
                {
                    float particleLastDensity2 = particle.LastDensity * particle.LastDensity;
                    Vector2 pressureAcceleration = Vector2.Zero;
                    foreach (Particle neighbour in particle.Neighbours)
                    {
                        if (particle.IsBoundaryParticle)
                        {
                            pressureAcceleration -= Gamma * neighbour.Mass * 2 * (particle.PredictedPressure / particleLastDensity2) * kernel.GradW(particle.Position, neighbour.Position);
                        }
                        else
                        {
                            float neighbourLastDensity2 = neighbour.LastDensity * neighbour.LastDensity;
                            float innerTerm = (particle.PredictedPressure / particleLastDensity2) + (neighbour.PredictedPressure / neighbourLastDensity2);
                            pressureAcceleration -= neighbour.Mass * innerTerm * kernel.GradW(particle.Position, neighbour.Position);
                        }
                    }
                    particle.PressureAcceleration = pressureAcceleration;
                }
            }
        }
    }
}
