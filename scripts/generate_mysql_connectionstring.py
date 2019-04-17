#!/usr/bin/env python3

import sys

def main():
	hostname = input("Database hostname: ")
	database = input("Database name: ")
	username = input("Username: ")
	password = input("Password: ")
	ssl = input("SSL/TLS [n]o/(y)es/(f)orce:")

	if (ssl.lower() == ""):
		ssl = "none"
	elif (ssl.lower() == "n"):
		ssl = "none"
	elif (ssl.lower() == "y"):
		ssl = "Preferred"
	elif (ssl.lower() == "f"):
		ssl = "Required"
	else:
		print("Invalid value for SSL/TLS: " + ssl)
		sys.exit(1)

	print("Server=%s;Database=%s;Uid=%s;Pwd=%s;SslMode=%s;" % (hostname, database, username, password, ssl))

if __name__ == "__main__":
    main()

