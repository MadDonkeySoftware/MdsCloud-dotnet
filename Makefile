# IMPORTANT:
# The ; and \ on various commands are important to keep all lines running in a single shell.
# The default behavior of a makefile has each line of a target run in an independent shell.
# When running commands that need to be in the venv the ; and \ are required to keep in the
# existing shell of the previous command.
# Reminder: a dash "-" in front of a command means errors will be ignored

.DEFAULT_GOAL := help
.PHONY := analysis autoformat clean clean-pyc dev-install dev-uninstall help lint requirements setup test upgrade-dependencies

autoformat:  ## Runs the auto formatting tool
	dotnet csharpier .

compile:  ## Compiles the application down to a dist folder
	ls | egrep "*.sln$$" | xargs -I {SLN} dotnet build {SLN}

dev-install:  ## Installs the app in a way that modifications to the files are run easily
	dotnet tool restore

format: autoformat  ## Alias to autoformat

identity-keys:  ## Creates a keypair for Identity to use locally
	echo 'foobarbaz' > pass; \
	rm -f ./key ./key.pub ./key.pub.pem; \
	ssh-keygen -f ./key -t rsa -b 4096 -m PKCS8 -n $$(cat pass) -N 'some-pass'; \
	ssh-keygen -f ./key.pub -e -m pem > key.pub.pem; \
	mkdir -p ./Identity/configs/keys; \
	cp -f ./key ./Identity/configs/keys; \
	cp -f ./key.pub ./Identity/configs/keys; \
	cp -f ./key.pub.pem ./Identity/configs/keys; \
	cp -f ./pass ./Identity/configs/keys; \
	rm -f ./key ./key.pub ./key.pub.pem ./pass

help:  ## Prints this help message.
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-30s\033[0m %s\n", $$1, $$2}'

list-updates:  ## Prints a list of packages per project that need updating
	ls | egrep "*.sln$$" | xargs -I {SLN} dotnet list {SLN} package --outdated

test: ## Runs the tox suite against each of the target interpreters.
	dotnet test ./MdsCloud.All.sln