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

format: autoformat  ## Alias to autoformat

help:  ## Prints this help message.
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-30s\033[0m %s\n", $$1, $$2}'

db-refresh: ## Runs the tox suite against each of the target interpreters.
	docker compose down -v && docker compose up -d && dotnet build ../DbTooling/DbTooling.csproj && ../DbTooling/bin/Debug/net7.0/DbTooling migrations run Identity
