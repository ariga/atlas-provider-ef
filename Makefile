.PHONY: build
build:
	dotnet build --configuration Release

.PHONY: pack
pack: build
	dotnet pack --configuration Release

.PHONY: clean
clean:
	dotnet clean
	rm -rf ./artifacts
	rm -rf ./src/**/bin
	rm -rf ./src/**/obj

.PHONY: sdks
sdks:
	sudo ./scripts/dotnet-install.sh --channel 8.0 --install-dir ${DOTNET_ROOT}
	sudo ./scripts/dotnet-install.sh --channel 6.0 --install-dir ${DOTNET_ROOT}
