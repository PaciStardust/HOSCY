{
  inputs.nixpkgs.url = "github:nixos/nixpkgs?ref=nixos-unstable";

  outputs =
    { self, nixpkgs, ... }:
    let
      pkgs = nixpkgs.legacyPackages.x86_64-linux;
    in
    {
      packages.x86_64-linux.default = pkgs.buildDotnetModule {
        pname = "hoscy";
        version = "0.1.0";

        src = ./.;
        projectFile = [ "Hoscy.sln" ];
        nugetDeps = ./flake-deps.json;

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

        executables = [
          "HscyCli"
          "HoscyUi"
          "HscyWhisperV2Process"
        ];

        fixupPhase = ''
          shopt -s globstar
          shopt -s nullglob
          rm -rf $out/**/{win,ios,maccatalyst,android,osx,freebsd,iossimulator,macos,linux-musl}-{arm,arm64,x86,x64}
        '';

        meta = {
          mainProgram = "HoscyUi";
        };
      };
    };
}
