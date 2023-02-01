SHELL:=/bin/bash

install_iotedgedev:
	sudo apt-get update
	sudo apt install make jq moreutils python3-pip
	sudo pip3 install virtualenv
	virtualenv venv
	source venv/bin/activate 
	pip3 install -U iotedgedev

install_azure_iot:
	curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
	az extension add --name azure-iot

install_qemu:
	sudo apt-get install qemu binfmt-support qemu-user-static
	docker run --rm --privileged multiarch/qemu-user-static --reset -p yes