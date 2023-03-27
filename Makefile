# IMPORTANT:
# The ; and \ on various commands are important to keep all lines running in a single shell.
# The default behavior of a makefile has each line of a target run in an independent shell.
# When running commands that need to be in the venv the ; and \ are required to keep in the
# existing shell of the previous command.
# Reminder: a dash "-" in front of a command means errors will be ignored

.DEFAULT_GOAL := help
.PHONY := analysis autoformat clean clean-pyc dev-install dev-uninstall help lint requirements setup test upgrade-dependencies

platform=$(shell uname -s)

autoformat:  ## Runs the auto formatting tool
	dotnet csharpier .

compile:  ## Compiles the application down to a dist folder
	ls | egrep ".sln$$" | xargs -I {SLN} dotnet build {SLN} --force --no-incremental

ifeq ($(platform),Darwin)
#TODO: Figure out Mac CLI install
dev-cli-install:
	@ln -s "$(shell pwd)/source/MdsCloud.CLI/bin/Debug/net7.0/MdsCloud.CLI" /usr/local/bin/mds

dev-cli-uninstall:
	@rm -f /usr/local/bin/mds
else
# NOTE: Linix specific (systemd based) config
dev-cli-install:  ## Installs the mds cli in a way that each build is reflected immediately
	@ln -s "$(shell pwd)/source/MdsCloud.CLI/bin/Debug/net7.0/MdsCloud.CLI" ~/.local/bin/mds

dev-cli-uninstall:  ## Removes the mds cli that was installed via the dev-cli-install make target
	@rm -f ~/.local/bin/mds
endif

dev-tool-install:  ## Installs the app in a way that modifications to the files are run easily
	dotnet tool restore

elk-stack-up:  ## Starts up the support / logging ELK stack
	docker compose -f ./docker-compose.elk.yml up -d

elk-stack-down:  ## Shuts down the support / logging ELK stack
	docker compose -f ./docker-compose.elk.yml down -v

format: autoformat  ## Alias to autoformat

identity-keys:  ## Creates a keypair for Identity to use locally
	echo 'foobarbaz' > pass; \
	rm -f ./key ./key.pub ./key.pub.pem ./nginx-selfsigned.crt ./nginx-selfsigned.key; \
	ssh-keygen -f ./key -t rsa -b 4096 -m PKCS8 -n $$(cat pass) -N 'some-pass'; \
	ssh-keygen -f ./key.pub -e -m pem > key.pub.pem; \
	openssl req -x509 -nodes -days 365 -newkey rsa:2048 -keyout nginx-selfsigned.key -out nginx-selfsigned.crt -batch -subj /; \
	mkdir -p ./source/MdsCloud.Identity/configs/keys ./source/MdsCloud.Identity/configs/ssh-proxy; \
	cp -f ./key ./source/MdsCloud.Identity/configs/keys; \
	cp -f ./key.pub ./source/MdsCloud.Identity/configs/keys; \
	cp -f ./key.pub.pem ./source/MdsCloud.Identity/configs/keys; \
	cp -f ./pass ./source/MdsCloud.Identity/configs/keys; \
	cp -f ./nginx-selfsigned.crt ./source/MdsCloud.Identity/configs/ssh-proxy; \
	cp -f ./nginx-selfsigned.key ./source/MdsCloud.Identity/configs/ssh-proxy; \
	rm -f ./key ./key.pub ./key.pub.pem ./pass ./nginx-selfsigned.key ./nginx-selfsigned.crt

help:  ## Prints this help message.
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-30s\033[0m %s\n", $$1, $$2}'

list-updates:  ## Prints a list of packages per project that need updating
	ls | egrep "*.sln$$" | xargs -I {SLN} dotnet list {SLN} package --outdated

test: ## Runs the tox suite against each of the target interpreters.
	dotnet test ./MdsCloud.All.sln
