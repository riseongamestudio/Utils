#!/bin/bash
# Builds Runtime/Generator/RiseOnUtilsGenerator.dll with the Roslyn compiler Unity ships, against netstandard 2.0 and
# Microsoft.CodeAnalysis 4.3, which Unity's manual asks source generators to use. Run after editing the generator.
set -e
cd "$(dirname "$0")"
UNITY="${UNITY:-C:/Program Files/Unity/Hub/Editor/6000.3.19f1/Editor}"
NUGET="${NUGET:-$HOME/.nuget/packages}"
mkdir -p ../Runtime/Generator
"$UNITY/Data/NetCoreRuntime/dotnet.exe" exec "$UNITY/Data/DotNetSdkRoslyn/csc.dll" \
  -nologo -nostdlib -noconfig -target:library -optimize+ -deterministic -langversion:9 \
  -out:../Runtime/Generator/RiseOnUtilsGenerator.dll \
  -reference:"$NUGET/netstandard.library/2.0.3/build/netstandard2.0/ref/netstandard.dll" \
  -reference:"$NUGET/microsoft.codeanalysis.common/4.3.0/lib/netstandard2.0/Microsoft.CodeAnalysis.dll" \
  -reference:"$NUGET/microsoft.codeanalysis.csharp/4.3.0/lib/netstandard2.0/Microsoft.CodeAnalysis.CSharp.dll" \
  -reference:"$NUGET/system.collections.immutable/6.0.0/lib/netstandard2.0/System.Collections.Immutable.dll" \
  RiseOnUtilsGenerator.cs