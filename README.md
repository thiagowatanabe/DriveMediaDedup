DriveMediaDedup Project

DOCUMENTAÇÃO DO SISTEMA

Manual de Configuração, Fluxo de Dados e README

23 de agosto de 2026

---

### 1. Arquivo README.md

Abaixo consta o conteúdo integral do arquivo README.md, estruturado para documentar todas as funcionalidades implementadas, incluindo a integração com Amazon S3, compactação de arquivos e rotinas de exclusão automática.

# 📸 DriveMediaDedup

Ferramenta em .NET 10 que conecta ao Google Drive, baixa imagens e vídeos, identifica imagens duplicadas (via hash SHA256), compacta os arquivos únicos, envia para o Amazon S3 e deleta os arquivos originais do Drive e locais.

---

## 🚀 O que o projeto faz

1. **Conecta ao Google Drive** via API oficial do Google
2. **Busca arquivos** de imagem (image/*) e vídeo (video/*)
3. **Faz o download** de todos os arquivos encontrados
4. **Compara imagens** usando hash SHA256:
   - Se a imagem é **duplicada** → move para a pasta `repetidas/`
   - Se a imagem é **única** → move para a pasta `organizadas/`
5. **Vídeos** vão direto para a pasta `videos/` (sem deduplicação)
6. **Compacta** apenas imagens únicas e vídeos em um arquivo zip (sem repetidas)
7. **Envia o zip** para o Amazon S3
8. **Deleta os arquivos** do Google Drive após upload
9. **Limpa os arquivos locais** após confirmação de upload no S3
10. **Cria o repositório** no GitHub automaticamente via API

---

## 🛠️ Pré-requisitos

Antes de começar, você precisa ter instalado:

- [.NET 10 SDK](https://dotnet.microsoft.com/download) — para compilar e rodar o projeto
- [Git](https://git-scm.com/downloads) — para controle de versão
- [GitHub CLI](https://cli.github.com/) — para autenticar e criar repositórios (opcional se já tem o token)

E ter contas configuradas em:

- **Google Cloud Console** — com Google Drive API ativada
- **Amazon Web Services (AWS)** — com um bucket S3 criado e Access Keys geradas
- **GitHub** — com Personal Access Token gerado

---

## 📦 Pacotes NuGet utilizados

| Pacote | Função |
|---|---|
| `Google.Apis.Drive.v3` | Acessar a API do Google Drive |
| `Google.Apis.Auth` | Autenticação OAuth2 com o Google |
| `System.Drawing.Common` | Manipulação de imagens |
| `Microsoft.Extensions.Configuration.Json` | Ler o arquivo appsettings.json |
| `Microsoft.Extensions.Hosting` | Injeção de dependência |
| `Octokit` | Criar repositório no GitHub via API |
| `AWSSDK.S3` | Upload de arquivos para o Amazon S3 |

---

## 🔧 Configuração passo a passo

### Passo 1 — Clonar o repositório

```bash
git clone https://github.com/SEU_USUARIO/DriveMediaDedup.git
cd DriveMediaDedup
```

### Passo 2 — Restaurar pacotes

```bash
dotnet restore
```

### Passo 3 — Criar o arquivo credentials.json (Google Drive API)

1. Acesse o [Google Cloud Console](https://console.cloud.google.com/)
2. Crie um projeto novo
3. Ative a **Google Drive API** (APIs e Serviços → Biblioteca → Google Drive API → Ativar)
4. Configure a **tela de consentimento OAuth**:
   - Tipo: Externo
   - Preencha nome do app, e-mail de suporte e e-mail do desenvolvedor
5. Crie a credencial OAuth 2.0:
   - APIs e Serviços → Credenciais → + Criar Credenciais → ID do cliente OAuth
   - Tipo de aplicação: **App para computador (Desktop App)**
   - Nome: `DriveMediaDedup`
6. Clique em **Baixar JSON**
7. Renomeie o arquivo para `credentials.json`
8. Coloque o arquivo na pasta do projeto:

```
src/DriveMediaDedup/credentials.json
```

> ⚠️ **Nunca suba o credentials.json para o GitHub!** Ele já está no .gitignore.

### Passo 4 — Adicionar usuário de teste no Google

Como o app está em modo de teste, você precisa adicionar seu e-mail como usuário autorizado:

1. Google Cloud Console → APIs e Serviços → **Tela de permissão OAuth**
2. Procure a seção **Usuários de teste**
3. Clique em **+ Add Users**
4. Adicione o e-mail da sua conta Google
5. Clique em **Salvar**

### Passo 5 — Criar o arquivo appsettings.json

Na pasta do projeto (src/DriveMediaDedup/), crie o arquivo appsettings.json com o seguinte conteúdo:

```json
{
  "GoogleDrive": {
    "CredentialsPath": "credentials.json",
    "ApplicationName": "DriveMediaDedup"
  },
  "Folders": {
    "Temp": "./downloads/temp",
    "Organized": "./downloads/organizadas",
    "Repeated": "./downloads/repetidas",
    "Videos": "./downloads/videos"
  },
  "S3": {
    "AccessKey": "SUA_ACCESS_KEY",
    "SecretKey": "SUA_SECRET_KEY",
    "BucketName": "seu-bucket-name",
    "Region": "sa-east-1"
  },
  "GitHub": {
    "Token": "COLE_SEU_TOKEN_AQUI",
    "Owner": "seu-usuario-github",
    "RepoName": "DriveMediaDedup",
    "Description": "Ferramenta para baixar e organizar mídias do Google Drive com deduplicação"
  }
}
```

Substitua:
- `SUA_ACCESS_KEY` → pela Access Key da AWS
- `SUA_SECRET_KEY` → pela Secret Key da AWS
- `seu-bucket-name` → pelo nome do seu bucket S3
- `sa-east-1` → pela região do seu bucket (ex: sa-east-1 para São Paulo)
- `COLE_SEU_TOKEN_AQUI` → pelo seu Personal Access Token do GitHub
- `seu-usuario-github` → pelo seu nome de usuário do GitHub

> ⚠️ **Nunca suba o appsettings.json para o GitHub!** Ele já está no .gitignore.

### Passo 6 — Gerar o Token do GitHub

1. Acesse: GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic)
2. Clique em **Generate new token (classic)**
3. Preencha:
   - **Note**: DriveMediaDedup
   - **Expiration**: 90 days
   - **Scopes**: marque `repo`
4. Clique em **Generate token**
5. Copie o token (começa com `ghp_`)
6. Cole no campo Token do appsettings.json

### Passo 7 — Configurar credenciais da AWS

1. Acesse o [AWS Console](https://console.aws.amazon.com/)
2. Crie um bucket S3:
   - Vá em S3 → Create bucket
   - Nome: `seu-bucket-name`
   - Região: escolha a mais próxima (ex: sa-east-1 para São Paulo)
3. Crie uma Access Key:
   - Vá em IAM → Users → seu usuário → Security credentials
   - Clique em **Create access key**
   - Copie a **Access Key ID** e a **Secret Access Key**
4. Cole os valores no `appsettings.json` nos campos `AccessKey` e `SecretKey`

### Passo 8 — Configurar o .csproj

O arquivo DriveMediaDedup.csproj deve conter:

```xml


  
    Exe
    net10.0
    enable
    enable
  

  
    
    
    
    
    
    
    
  

  
    
      PreserveNewest
    
    
      PreserveNewest
    
  


```

---

## ▶️ Como executar o projeto

```bash
cd src/DriveMediaDedup
dotnet build
dotnet run
```

### O que acontece na primeira execução:

1. O programa vai abrir o **navegador** pedindo permissão para acessar seu Google Drive (com permissão de leitura E exclusão)
2. Você clica em **Permitir**
3. O Google salva um token na pasta `token.json/` (para não pedir permissão de novo)
4. O programa busca imagens e vídeos no Drive
5. Faz o download de cada arquivo
6. Compara as imagens (hash SHA256) — separa únicas vs repetidas
7. Compacta apenas organizadas + vídeos em um arquivo zip (sem repetidas)
8. Envia o zip para o Amazon S3
9. Deleta os arquivos originais do Google Drive
10. Deleta os arquivos locais (apenas se o upload S3 teve sucesso)
11. Cria o repositório no GitHub (se ainda não existir)
12. Mostra um relatório final

> ⚠️ **Importante:** Na primeira execução com o novo scope (Drive), o Google vai pedir nova autorização. O token antigo deve ser deletado:
> ```bash
> rm -rf src/DriveMediaDedup/bin/Debug/net10.0/token.json
> ```

---

## 📁 Estrutura de pastas do projeto

```
DriveMediaDedup/
├── src/
│   └── DriveMediaDedup/
│       ├── DriveMediaDedup.csproj
│       ├── Program.cs
│       ├── appsettings.json
│       ├── credentials.json
│       ├── Services/
│       │   ├── GoogleDriveService.cs   ← conexão, busca, download e exclusão no Drive
│       │   ├── DownloadService.cs      ← download dos arquivos
│       │   ├── DedupService.cs         ← deduplicação de imagens (SHA256)
│       │   ├── ZipService.cs           ← compactação dos arquivos
│       │   ├── S3Service.cs            ← upload para o Amazon S3
│       │   └── GitHubService.cs        ← criação do repo no GitHub
│       ├── Models/
│       │   └── MediaFile.cs            ← modelo de dados
│       └── Config/
│           └── appsettings.json       ← template de configuração
├── .gitignore
├── README.md
└── DriveMediaDedup.slnx
```

---

## 📂 Pastas geradas na execução

| Pasta | Conteúdo | Após execução |
|---|---|---|
| `downloads/temp/` | Downloads em andamento | Excluída |
| `downloads/organizadas/` | Imagens únicas (sem repetição) | Excluída |
| `downloads/repetidas/` | Imagens duplicadas | Excluída |
| `downloads/videos/` | Todos os vídeos baixados | Excluída |
| `downloads/zips/` | Arquivo zip gerado | Excluída |

> Todas as pastas locais são excluídas após confirmação de upload no S3. Se o upload falhar, os arquivos são mantidos como backup.

---

## 🔒 Arquivos que NÃO devem ir para o GitHub

| Arquivo | Por quê |
|---|---|
| `credentials.json` | Contém as chaves do Google |
| `appsettings.json` | Contém o token do GitHub e credenciais da AWS |
| `token.json/` | Contém o token de acesso OAuth do Google |

Todos eles já estão listados no .gitignore.

---

## 🧠 Como funciona a deduplicação

A deduplicação usa **hash SHA256** — uma "impressão digital" única de cada arquivo:

1. O programa calcula o hash de cada imagem baixada
2. Compara com os hashes já conhecidos
3. Se o hash **já existe** → a imagem é **duplicada** → vai para `repetidas/`
4. Se o hash **não existe** → a imagem é **única** → vai para `organizadas/` e o hash é registrado

> As imagens duplicadas NÃO são incluídas no zip enviado ao S3. Apenas imagens únicas e vídeos são compactados e enviados.

---

## 🔄 Fluxo completo de execução

```
1. Busca arquivos no Drive (imagens + vídeos)
2. Download de cada arquivo
3. Deduplicação (SHA256):
   ├── Imagem única → organizadas/
   ├── Imagem repetida → repetidas/
   └── Vídeo → videos/
4. Compacta organizadas/ + videos/ em um zip (SEM repetidas)
5. Upload do zip para o Amazon S3
6. Deleta arquivos originais do Google Drive
7. Limpa arquivos locais (se S3 teve sucesso)
8. Cria repositório no GitHub (se não existir)
9. Mostra relatório final
```

---

## 🗑️ Segurança de exclusão

O projeto tem **três camadas de segurança** antes de excluir qualquer arquivo:

| Condição | O que acontece |
|---|---|
| Download falhou | Arquivo não é deletado do Drive |
| Upload S3 falhou | Arquivos locais são mantidos como backup |
| Upload S3 teve sucesso | Arquivos locais são excluídos |

> Os arquivos do Drive só são deletados se o download foi bem-sucedido. Os arquivos locais só são excluídos se o upload S3 foi bem-sucedido.

---

## 🐙 Para que serve a classe GitHubService

A classe GitHubService cria o repositório no GitHub automaticamente via código, sem precisar abrir o navegador.

### Métodos:

- `CreateRepositoryAsync()` — Cria um repositório novo no GitHub com nome e descrição do appsettings.json
- `RepositoryExistsAsync()` — Verifica se o repositório já existe (para não criar duplicado)

### Como funciona:

1. O programa roda toda a parte de download, deduplicação, zip, S3 e limpeza
2. No final, chama o GitHubService
3. Ele verifica se o repositório já existe
4. Se não existir, cria automaticamente usando o token (Personal Access Token)
5. O token é como uma senha que autoriza o código a criar repositórios em seu nome

---

## 📦 Para que servem as novas classes

### ZipService

Compacta apenas as pastas `organizadas/` e `videos/` em um único arquivo zip. A pasta `repetidas/` não é incluída.

### S3Service

Faz o upload do arquivo zip gerado para o Amazon S3 usando as credenciais configuradas no appsettings.json.

---

## 📋 Relatório final

Ao final da execução, o programa mostra:

```
=== RELATÓRIO FINAL ===
Total baixado:       150
Imagens únicas:      87
Imagens repetidas:    45 (mantidas localmente, não foram para o S3)
Vídeos:              18
Arquivo zip:         DriveMediaDedup_20260823_153000.zip (organizadas + vídeos)
Upload S3:           Concluído
Deletados do Drive:   150
Arquivos locais:      Excluídos
```

---

## 📝 Licença

Este projeto está sob a licença MIT. Sinta-se livre para usar e modificar.

---

*Documento elaborado em 23 de agosto de 2026. As informações contidas são de responsabilidade do solicitante.*
