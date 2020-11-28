#!/bin/bash
# Used to quickly slice a file so it can be shown off in presentations and/or reports
# Takes about five seconds total.

./transformer/boidsTransformer/bin/Release/net5.0/boidsTransformer.exe ./datasets/raw_boids_ts/long.boids -o ./datasets/proc_boids_ts -s 65

cp ./datasets/raw_boids_ts/long.boids ./datasets/raw_boids_ts/long2.boids
./transformer/boidsTransformer/bin/Release/net5.0/boidsTransformer.exe ./datasets/raw_boids_ts/long2.boids -o ./datasets/proc_boids_ts -s 65 -t 2
rm ./datasets/raw_boids_ts/long2.boids

cp ./datasets/raw_boids_ts/long.boids ./datasets/raw_boids_ts/long4.boids
./transformer/boidsTransformer/bin/Release/net5.0/boidsTransformer.exe ./datasets/raw_boids_ts/long4.boids -o ./datasets/proc_boids_ts -s 65 -t 4
rm ./datasets/raw_boids_ts/long4.boids

cp ./datasets/raw_boids_ts/long.boids ./datasets/raw_boids_ts/long8.boids
./transformer/boidsTransformer/bin/Release/net5.0/boidsTransformer.exe ./datasets/raw_boids_ts/long8.boids -o ./datasets/proc_boids_ts -s 65 -t 8
rm ./datasets/raw_boids_ts/long8.boids

cp ./datasets/raw_boids_ts/long.boids ./datasets/raw_boids_ts/long16.boids
./transformer/boidsTransformer/bin/Release/net5.0/boidsTransformer.exe ./datasets/raw_boids_ts/long16.boids -o ./datasets/proc_boids_ts -s 65 -t 16
rm ./datasets/raw_boids_ts/long16.boids

cp ./datasets/raw_boids_ts/long.boids ./datasets/raw_boids_ts/long32.boids
./transformer/boidsTransformer/bin/Release/net5.0/boidsTransformer.exe ./datasets/raw_boids_ts/long32.boids -o ./datasets/proc_boids_ts -s 65 -t 32
rm ./datasets/raw_boids_ts/long32.boids

cp ./datasets/raw_boids_ts/long.boids ./datasets/raw_boids_ts/long64.boids
./transformer/boidsTransformer/bin/Release/net5.0/boidsTransformer.exe ./datasets/raw_boids_ts/long64.boids -o ./datasets/proc_boids_ts -s 65 -t 64
rm ./datasets/raw_boids_ts/long64.boids
