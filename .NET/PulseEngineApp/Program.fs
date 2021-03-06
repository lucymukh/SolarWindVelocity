﻿module app

open PulseEngine
open Angara.Data

let loadPulses (csvFile:string) =
    Table.Load(csvFile) |>
    Table.Map ["t";"power";"velocity"] (fun (t:float) (p:float) (v:float) ->
        {
            EmergenceTime=t;
            Power=p;
            Velocity=v;
            Kernel=Kernels.GaussianExt 3.0;
            A=v
        }
    ) |> List.ofSeq

[<EntryPoint>]
let main argv = 
    Angara.Base.Init()
    
    let testWind = loadPulses @"..\..\..\..\TestData\3pulses.csv"

    let time_start = 0.0
    let time_step = 0.1
    let space_step = 0.1
    let space_start = 0.0
    let space_end = 90.0
    
    let simulation = simulate testWind time_start 60.0 time_step space_start space_end space_step

    //Dumping the data to NetCDF using http://research.microsoft.com/en-us/downloads/ccf905f6-34c6-4845-892e-a5715a508fa3/
    let ds = Microsoft.Research.Science.Data.DataSet.Open("msds:nc?openMode=create&file=simulation.nc")
    ds.IsAutocommitEnabled <- false
    let windVar = ds.AddVariable<float>("windDens",[|"x";"t"|])
    let windSpVar = ds.AddVariable<float>("avgWindSpeed",[|"x";"t"|])
    let windMaxSpVar = ds.AddVariable<float>("maxWindSpeed",[|"x";"t"|])
    let timeAxis = ds.AddVariable<float>("t",[|"t"|])
    let spaceAxis = ds.AddVariable<float>("x",[|"x"|])    

    List.iteri (fun i sim_step_data ->
        let sample_vals,sampls_speed,sample_max_speed = sim_step_data
        let t = time_start+float(i)*time_step
        windVar.Append(List.toArray sample_vals,"t")
        windSpVar.Append(List.toArray sampls_speed,"t")
        windMaxSpVar.Append(List.toArray sample_max_speed,"t")
        timeAxis.Append([|t|]);
        )
        simulation
    let spaceAxisData = Array.init (int((space_end-space_start)/space_step)) (fun i -> space_start+float(i)*space_step)
    spaceAxis.Append(spaceAxisData)

    ds.Commit()        
    0 // return an integer exit code
