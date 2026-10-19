{
  inputs.nixpkgs.url = "github:nixos/nixpkgs?ref=nixos-unstable";

  outputs =
    { self, nixpkgs, ... }:
    let
      pkgs = nixpkgs.legacyPackages.x86_64-linux;

      create = { pname, projectFile, executables, mainProgram, nugetDeps } : pkgs.buildDotnetModule {
        inherit pname;

        version = "0.1.0";

        src = ./.;
        projectFile = projectFile ++ [ "./HoscyCore/HoscyCore.csproj" "./HoscyWhisperV2Process/HoscyWhisperV2Process.csproj" ];
        nugetDeps = nugetDeps;

        runtimeDeps = with pkgs; [
          libGL
          kdePackages.wayland
          fcitx5
          libxkbcommon
          miniaudio
          pulseaudio
        ];

        dotnet-sdk = pkgs.dotnetCorePackages.sdk_10_0;
        dotnet-runtime = pkgs.dotnetCorePackages.runtime_10_0;

        executables = executables ++ [ "HscyWhisperV2Process" ];

        fixupPhase = ''
          shopt -s globstar
          shopt -s nullglob
          rm -rf $out/**/{win,ios,maccatalyst,android,osx,freebsd,iossimulator,macos,linux-musl}-{arm,arm64,x86,x64}
        '';

        meta = {
          mainProgram = mainProgram;
        };
      };
    in
    {
      packages.x86_64-linux.default = create 
      { 
        pname = "HoscyUi";
        projectFile = [ "./HoscyAvaloniaUi/HoscyAvaloniaUi.csproj" ];
        executables = [ "HoscyUi" ];
        mainProgram = "HoscyUi";
        nugetDeps = ./flake-deps-ui.json;
      };
      packages.x86_64-linux.cli = create 
      { 
        pname = "HoscyCli";
        projectFile = [ "./HoscyCli/HoscyCli.csproj" ];
        executables = [ "HscyCli" ];
        mainProgram = "HscyCli";
        nugetDeps = ./flake-deps-cli.json;
      };
    };
}
