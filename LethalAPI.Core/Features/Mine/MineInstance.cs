﻿// -----------------------------------------------------------------------
// <copyright file="MineInstance.cs" company="LethalAPI Modding Community">
// Copyright (c) LethalAPI Modding Community. All rights reserved.
// Licensed under the GPL-3.0 license.
// </copyright>
// -----------------------------------------------------------------------

// ReSharper disable once CheckNamespace
namespace LethalAPI.Core.Features;

using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Contains the instance implementations for the mine.
/// </summary>
public partial class Mine : Hazard<Landmine, Mine>
{
    private Mine(GameObject mine)
        : base(mine)
    {
    }

    /// <summary>
    /// Gets the transform of the mine.
    /// </summary>
    public Transform Transform => Base.transform;

    /// <summary>
    /// Gets or sets the position of the mine.
    /// </summary>
    public Vector3 Position
    {
        get => Transform.position;
        set => Transform.position = value;
    }

    /// <summary>
    /// Gets or sets the rotation of the mine.
    /// </summary>
    public Quaternion Rotation
    {
        get => Transform.rotation;
        set => Transform.rotation = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether or not the mine is disarmed.
    /// </summary>
    public bool IsDisarmed
    {
        get => !Base.mineActivated;
        set => Base.mineActivated = !value;
    }

    /// <summary>
    /// Makes a mine explode.
    /// </summary>
    public void Explode()
    {
        Base.TriggerMineOnLocalClientByExiting();
    }

    /// <summary>
    /// Spawns the mine on the network.
    /// </summary>
    public void Spawn()
    {
        this.GameObject.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);
    }
}