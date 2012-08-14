﻿/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyrightD
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
using System;
using System.Collections.Generic;
using System.Text;
using log4net;
using OpenMetaverse;

namespace OpenSim.Region.Physics.BulletSPlugin
{

public class BSConstraintCollection : IDisposable
{
    // private static readonly ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    // private static readonly string LogHeader = "[CONSTRAINT COLLECTION]";

    delegate bool ConstraintAction(BSConstraint constrain);

    private List<BSConstraint> m_constraints;
    private BulletSim m_world;

    public BSConstraintCollection(BulletSim world)
    {
        m_world = world;
        m_constraints = new List<BSConstraint>();
    }

    public void Dispose()
    {
        this.Clear();
    }

    public void Clear()
    {
        foreach (BSConstraint cons in m_constraints)
        {
            cons.Dispose();
        }
        m_constraints.Clear();
    }

    public bool AddConstraint(BSConstraint cons)
    {
        // There is only one constraint between any bodies. Remove any old just to make sure.
        RemoveAndDestroyConstraint(cons.Body1, cons.Body2);

        m_world.scene.DetailLog("{0},BSConstraintCollection.AddConstraint,call,body1={1},body2={2}", BSScene.DetailLogZero, cons.Body1.ID, cons.Body2.ID);

        m_constraints.Add(cons);

        return true;
    }

    // Get the constraint between two bodies. There can be only one.
    // Return 'true' if a constraint was found.
    public bool TryGetConstraint(BulletBody body1, BulletBody body2, out BSConstraint returnConstraint)
    {
        bool found = false;
        BSConstraint foundConstraint = null;

        uint lookingID1 = body1.ID;
        uint lookingID2 = body2.ID;
        ForEachConstraint(delegate(BSConstraint constrain)
        {
            if ((constrain.Body1.ID == lookingID1 && constrain.Body2.ID == lookingID2)
                || (constrain.Body1.ID == lookingID2 && constrain.Body2.ID == lookingID1))
            {
                foundConstraint = constrain;
                found = true;
            }
            return found;
        });
        returnConstraint = foundConstraint;
        return found;
    }

    // Remove any constraint between the passed bodies.
    // Presumed there is only one such constraint possible.
    // Return 'true' if a constraint was found and destroyed.
    public bool RemoveAndDestroyConstraint(BulletBody body1, BulletBody body2)
    {
        // return BulletSimAPI.RemoveConstraint(m_world.ID, obj1.ID, obj2.ID);

        bool ret = false;
        BSConstraint constrain;

        if (this.TryGetConstraint(body1, body2, out constrain))
        {
            m_world.scene.DetailLog("{0},BSConstraintCollection.RemoveAndDestroyConstraint,taint,body1={1},body2={2}", BSScene.DetailLogZero, body1.ID, body2.ID);
            // remove the constraint from our collection
            m_constraints.Remove(constrain);
            // tell the engine that all its structures need to be freed
            constrain.Dispose();
            // we destroyed something
            ret = true;
        }

        return ret;
    }

    // Remove all constraints that reference the passed body.
    // Return 'true' if any constraints were destroyed.
    public bool RemoveAndDestroyConstraint(BulletBody body1)
    {
        // return BulletSimAPI.RemoveConstraintByID(m_world.ID, obj.ID);

        List<BSConstraint> toRemove = new List<BSConstraint>();
        uint lookingID = body1.ID;
        ForEachConstraint(delegate(BSConstraint constrain)
        {
            if (constrain.Body1.ID == lookingID || constrain.Body2.ID == lookingID)
            {
                toRemove.Add(constrain);
            }
            return false;
        });
        lock (m_constraints)
        {
            foreach (BSConstraint constrain in toRemove)
            {
                m_constraints.Remove(constrain);
                constrain.Dispose();
            }
        }
        return (toRemove.Count > 0);
    }

    public bool RecalculateAllConstraints()
    {
        ForEachConstraint(delegate(BSConstraint constrain)
        {
            constrain.CalculateTransforms();
            return false;
        });
        return true;
    }

    // Lock the constraint list and loop through it.
    // The constraint action returns 'true' if it wants the loop aborted.
    private void ForEachConstraint(ConstraintAction action)
    {
        lock (m_constraints)
        {
            foreach (BSConstraint constrain in m_constraints)
            {
                if (action(constrain))
                    break;
            }
        }
    }


}
}
