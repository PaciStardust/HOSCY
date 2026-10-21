{
  inputs.nixpkgs.url = "github:nixos/nixpkgs?ref=nixos-unstable";

  outputs =
    { self, nixpkgs, ... } @ inputs:
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
          fontconfig
          miniaudio
          pulseaudio
          nix-ld
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

      packageUi = create 
      { 
        pname = "HoscyUi";
        projectFile = [ "./HoscyAvaloniaUi/HoscyAvaloniaUi.csproj" ];
        executables = [ "HoscyUi" ];
        mainProgram = "HoscyUi";
        nugetDeps = ./flake-deps-ui.json;
      };
      packageCli = create 
      { 
        pname = "HoscyCli";
        projectFile = [ "./HoscyCli/HoscyCli.csproj" ];
        executables = [ "HscyCli" ];
        mainProgram = "HscyCli";
        nugetDeps = ./flake-deps-cli.json;
      };
    in
    {
      nixosConfigurations.test-vm = nixpkgs.lib.nixosSystem {
        specialArgs = { inherit inputs; };
        modules = [
          {
            networking.hostName = "hoscy-test";
            system.stateVersion = "25.11";
            users.users.test = {
              isNormalUser = true;
              initialPassword = "test";
              description = "Test-Account";
              extraGroups = [ "networkmanager" "wheel" ];
            };
            virtualisation.vmVariant = {
              virtualisation.memorySize = 16384;
              virtualisation.cores = 8;
              virtualisation.diskSize = 100000;
            };
            boot.loader.systemd-boot.enable = true;
            boot.loader.efi.canTouchEfiVariables = true;
            security.sudo = {
              enable = true;
              wheelNeedsPassword = true;
            };
            networking.networkmanager.enable = true;
            time.timeZone = "Europe/Berlin";
            i18n.defaultLocale = "en_GB.UTF-8";
            i18n.extraLocaleSettings = {
              LC_ADDRESS = "de_DE.UTF-8";
              LC_IDENTIFICATION = "en_GB.UTF-8";
              LC_MEASUREMENT = "de_DE.UTF-8";
              LC_MONETARY = "de_DE.UTF-8";
              LC_NAME = "de_DE.UTF-8";
              LC_NUMERIC = "de_DE.UTF-8";
              LC_PAPER = "de_DE.UTF-8";
              LC_TELEPHONE = "de_DE.UTF-8";
              LC_TIME = "en_GB.UTF-8";
            };
            console.keyMap = "us";
            nix.settings.experimental-features = [ "nix-command" "flakes" ];
            environment.systemPackages = with pkgs; [
              curl
              git
              man-db
              man-pages
              neovim
              wget
              packageUi
              packageCli
              strace
            ];
            services.xserver.enable = true;
            services.xserver.xkb = {
              layout = "us";
              variant = "altgr-intl";
              model = "pc105";
            };
            hardware.graphics.enable = true;
            services.displayManager.gdm.enable = true;
            services.desktopManager.gnome.enable = true;
            services.gnome.core-developer-tools.enable = false;
            services.gnome.games.enable = false;
            environment.gnome.excludePackages = with pkgs; [ gnome-tour gnome-user-docs ];
            nixpkgs.hostPlatform = nixpkgs.lib.mkDefault "x86_64-linux";
          }
        ];
      };
      packages.x86_64-linux.default = packageUi;
      packages.x86_64-linux.cli = packageCli;
    };
}