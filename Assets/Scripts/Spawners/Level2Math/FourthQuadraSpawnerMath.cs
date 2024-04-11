using System;
using Spawners;
using UnityEngine;

[CreateAssetMenu()]
public class FourthQuadraSpawnerMath :  MathSpawnSO
{
    public override int GetNumberMerdeToSpawn(int turn)
    {
        return (int) Math.Ceiling(turn *0.28);
    }

    public override int GetBigGuyToSpawn(int turn)
    {
        return (int) Math.Round((turn *0.3) /1.2 );
    }

    public override int GetDoggoToSpawn(int turn)
    {
        return (int)Math.Round((turn * 0.1));
    }

    public override int GetSnipperToSpawn(int turn)
    {
        return 0;
    }
}