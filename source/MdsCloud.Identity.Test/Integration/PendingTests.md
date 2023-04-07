# Pending Tests

* Authentication
  * With simple signing, succeeds when user credentials valid
    * Currently only RSA Signing implemented
* Public Signature
  * Returns empty response when no public signature available
* Update User Controller
  * Can successfully update user details
  * Fails when updating password and old password does not match
  * Fails when no fields provided to update
  * Fails when user is not active
  * Fails when user is not found
  * Fails when no token provided
  * Fails when token is invalid