# Private Key Authentication

WebSSH now supports SSH private key authentication in addition to password authentication. This allows you to connect to remote servers using your SSH private keys, which is often more secure and convenient than using passwords.

## Features

- **Multiple Authentication Types**: Choose between password or private key authentication for each stored session
- **Encrypted Key Support**: Supports private keys protected with a passphrase
- **Multiple Key Formats**: Compatible with RSA, DSA, ECDSA, and ED25519 key types
- **Secure Storage**: Private keys are stored as base64-encoded strings in your browser's local storage
- **Backward Compatible**: Existing password-based sessions continue to work without any changes

## Using Private Key Authentication

### Adding a Session with Private Key

1. Navigate to the **Management** page
2. In the "Add or Edit Session Info" section:
   - Fill in the **Display Name**, **Host**, **Port**, and **User Name**
   - Select **Private Key** from the **Authentication Type** dropdown
   - Paste your private key content into the **Private Key** textarea
     - For example: `-----BEGIN RSA PRIVATE KEY-----\n...\n-----END RSA PRIVATE KEY-----`
   - If your key is encrypted with a passphrase, enter it in the **Private Key Passphrase** field
   - Leave the passphrase field empty if your key is not encrypted
3. Click **Save Session Info**

### Connecting with a Stored Private Key Session

1. From the **Management** page, click **Connected** on any stored session that uses private key authentication
2. The session will connect using your stored private key
3. If the connection is successful, you'll be redirected to the shell interface

### Quick Connect with Private Key

1. Navigate to the **Connected** page
2. Fill in the connection details (Host, Port, User Name)
3. Select **Private Key** from the **Authentication Type** dropdown
4. Paste your private key and optionally enter the passphrase
5. Click **Connected**

## Generating SSH Keys

If you don't have an SSH key pair yet, you can generate one using `ssh-keygen`:

```bash
# Generate an RSA key pair
ssh-keygen -t rsa -b 4096 -C "your_email@example.com"

# Generate an ED25519 key pair (recommended)
ssh-keygen -t ed25519 -C "your_email@example.com"
```

The private key will be saved to `~/.ssh/id_rsa` (or `~/.ssh/id_ed25519` for ED25519).
The public key will be saved with a `.pub` extension.

## Adding Your Public Key to Remote Server

Before you can use private key authentication, you need to add your public key to the remote server:

```bash
# Copy your public key to the remote server
ssh-copy-id username@remote-host

# Or manually append it to ~/.ssh/authorized_keys on the remote server
cat ~/.ssh/id_rsa.pub | ssh username@remote-host "mkdir -p ~/.ssh && cat >> ~/.ssh/authorized_keys"
```

## Security Considerations

- **Local Storage**: Private keys are stored base64-encoded in your browser's local storage. While this provides basic obfuscation, it's not encryption. Anyone with access to your browser's storage can retrieve the keys.
- **HTTPS**: Always access WebSSH over HTTPS in production to protect your private keys during transmission
- **Key Passphrases**: Using a passphrase on your private keys adds an extra layer of security
- **Regular Rotation**: Regularly rotate your SSH keys and revoke old ones
- **Export/Import**: Be careful when exporting sessions as they contain your encoded private keys

## Supported Key Formats

WebSSH supports the following SSH key formats through the SSH.NET library:

- RSA (PKCS#1, OpenSSH format)
- DSA
- ECDSA (256, 384, 521 bit)
- ED25519

## Troubleshooting

### "Private key content is required for private key authentication"
This error occurs when the private key field is empty. Make sure you've pasted your entire private key, including the header and footer lines (e.g., `-----BEGIN RSA PRIVATE KEY-----` and `-----END RSA PRIVATE KEY-----`).

### Connection Fails with Private Key
- Verify that your public key is in the remote server's `~/.ssh/authorized_keys` file
- Check that the remote server has SSH key authentication enabled
- If your key has a passphrase, make sure you've entered it correctly
- Verify that your private key format is supported

### "Invalid private key file"
This usually indicates:
- The key format is not recognized or supported
- The key is corrupted
- The passphrase is incorrect (for encrypted keys)

## Example Private Key Structure

A valid RSA private key looks like this:

```
-----BEGIN RSA PRIVATE KEY-----
MIIEpAIBAAKCAQEA...
(multiple lines of base64-encoded data)
...
-----END RSA PRIVATE KEY-----
```

An ED25519 key looks like this:

```
-----BEGIN OPENSSH PRIVATE KEY-----
b3BlbnNzaC1rZXktdjEAAAAACmFlczI1Ni1jdHIAAAAGYmNyeXB0AAAAGAAAABB...
(multiple lines of base64-encoded data)
...
-----END OPENSSH PRIVATE KEY-----
```

Make sure to copy the entire content including the header and footer lines.
