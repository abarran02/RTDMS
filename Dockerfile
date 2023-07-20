FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim

# https://www.youtube.com/watch?v=0H2miBK_gAk
WORKDIR /code

# install Node.js
# https://stackoverflow.com/a/57546198
ENV NODE_VERSION=18.17.0
RUN apt-get update && apt-get install -y curl openssh-server
RUN curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.0/install.sh | bash
ENV NVM_DIR=/root/.nvm
RUN . "$NVM_DIR/nvm.sh" && nvm install ${NODE_VERSION}
RUN . "$NVM_DIR/nvm.sh" && nvm use v${NODE_VERSION}
RUN . "$NVM_DIR/nvm.sh" && nvm alias default v${NODE_VERSION}
ENV PATH="/root/.nvm/versions/node/v${NODE_VERSION}/bin/:${PATH}"
RUN node --version
RUN npm --version

# install Azure CLI
RUN curl -sL https://aka.ms/InstallAzureCLIDeb | bash
RUN az version

# install Azure Functions Core Tools
RUN curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg
RUN mv microsoft.gpg /etc/apt/trusted.gpg.d/microsoft.gpg
RUN sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/debian/$(lsb_release -rs | cut -d'.' -f 1)/prod $(lsb_release -cs) main" > /etc/apt/sources.list.d/dotnetdev.list'
RUN apt-get update && apt-get install -y azure-functions-core-tools-4
