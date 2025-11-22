#!/bin/bash

###############################################################################
# NavTrack Production Deployment Script for Linux
# This script automates the installation and configuration of:
# - Backend API (Navtrack.Api)
# - Frontend Web Application
# - Odoo Integration API (Odoo.Navtrac.Api)
# - GPS Listener Service (Navtrack.Listener)
# - MongoDB Database
# - Nginx Reverse Proxy
# - Let's Encrypt SSL Certificates
###############################################################################

set -e  # Exit on error

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration Variables
DOMAIN_FRONTEND="gps-qa.inversionespereztaveras.com"
DOMAIN_API="gps-api-qa.inversionespereztaveras.com"
DOMAIN_ODOO_API="gps-odoo-qa.inversionespereztaveras.com"
DOMAIN_LISTENER="gps-listener-qa.inversionespereztaveras.com"
EMAIL="admin@inversionespereztaveras.com"  # Change this to your email
MONGO_DATABASE="navtrack"
INSTALL_DIR="/opt/navtrack"
LISTENER_PORT_START=7002
LISTENER_PORT_END=7100

###############################################################################
# Helper Functions
###############################################################################

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

print_info() {
    echo -e "${YELLOW}ℹ $1${NC}"
}

check_root() {
    if [ "$EUID" -ne 0 ]; then
        print_error "This script must be run as root. Please use sudo."
        exit 1
    fi
}

###############################################################################
# Installation Functions
###############################################################################

install_docker() {
    print_info "Checking Docker installation..."

    if command -v docker &> /dev/null; then
        print_success "Docker is already installed ($(docker --version))"
        return
    fi

    print_info "Installing Docker..."

    # Update package index
    apt-get update

    # Install prerequisites
    apt-get install -y \
        ca-certificates \
        curl \
        gnupg \
        lsb-release

    # Add Docker's official GPG key
    mkdir -p /etc/apt/keyrings
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg

    # Set up the repository
    echo \
        "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
        $(lsb_release -cs) stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null

    # Install Docker Engine
    apt-get update
    apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

    # Start and enable Docker
    systemctl start docker
    systemctl enable docker

    print_success "Docker installed successfully"
}

install_nginx() {
    print_info "Checking Nginx installation..."

    if command -v nginx &> /dev/null; then
        print_success "Nginx is already installed ($(nginx -v 2>&1))"
        return
    fi

    print_info "Installing Nginx..."
    apt-get update
    apt-get install -y nginx

    # Start and enable Nginx
    systemctl start nginx
    systemctl enable nginx

    print_success "Nginx installed successfully"
}

install_certbot() {
    print_info "Checking Certbot installation..."

    if command -v certbot &> /dev/null; then
        print_success "Certbot is already installed"
        return
    fi

    print_info "Installing Certbot..."
    apt-get update
    apt-get install -y certbot python3-certbot-nginx

    print_success "Certbot installed successfully"
}

###############################################################################
# Configuration Functions
###############################################################################

create_docker_compose() {
    print_info "Creating production docker-compose.yml..."

    mkdir -p "$INSTALL_DIR"
    cd "$INSTALL_DIR"

    cat > docker-compose.prod.yml <<EOF
version: '3.8'

services:
  frontend:
    build:
      context: .
      dockerfile: frontend/Dockerfile
    container_name: navtrack-frontend
    restart: unless-stopped
    networks:
      - navtrack
    environment:
      - NAVTRACK_API_URL=https://${DOMAIN_API}
      - NAVTRACK_LISTENER_HOSTNAME=${DOMAIN_LISTENER}
      - NAVTRACK_LISTENER_IP=auto
    ports:
      - "127.0.0.1:3000:3000"
    depends_on:
      - api

  api:
    build:
      context: .
      dockerfile: backend/Navtrack.Api/Dockerfile
    container_name: navtrack-api
    restart: unless-stopped
    networks:
      - navtrack
    environment:
      - MongoOptions__Database=${MONGO_DATABASE}
      - MongoOptions__ConnectionString=mongodb://database:27017
      - ASPNETCORE_URLS=http://+:8080
      - ASPNETCORE_ENVIRONMENT=Production
    ports:
      - "127.0.0.1:8080:8080"
    depends_on:
      - database

  odoo-api:
    build:
      context: .
      dockerfile: Odoo.Navtrac.Api/Dockerfile
    container_name: navtrack-odoo-api
    restart: unless-stopped
    networks:
      - navtrack
    environment:
      - MongoOptions__Database=${MONGO_DATABASE}
      - MongoOptions__ConnectionString=mongodb://database:27017
      - ASPNETCORE_URLS=http://+:8080
      - ASPNETCORE_ENVIRONMENT=Production
    ports:
      - "127.0.0.1:8081:8080"
    depends_on:
      - database

  listener:
    build:
      context: .
      dockerfile: backend/Navtrack.Listener/Dockerfile
    container_name: navtrack-listener
    restart: unless-stopped
    networks:
      - navtrack
    environment:
      - MongoOptions__Database=${MONGO_DATABASE}
      - MongoOptions__ConnectionString=mongodb://database:27017
      - ASPNETCORE_ENVIRONMENT=Production
    ports:
      - "${LISTENER_PORT_START}-${LISTENER_PORT_END}:${LISTENER_PORT_START}-${LISTENER_PORT_END}"
    depends_on:
      - database

  database:
    image: mongo:latest
    container_name: navtrack-mongodb
    restart: unless-stopped
    networks:
      - navtrack
    volumes:
      - mongodb_data:/data/db
    environment:
      - MONGO_INITDB_DATABASE=${MONGO_DATABASE}
    ports:
      - "127.0.0.1:27017:27017"

volumes:
  mongodb_data:
    driver: local

networks:
  navtrack:
    driver: bridge
EOF

    print_success "Docker compose file created at $INSTALL_DIR/docker-compose.prod.yml"
}

configure_nginx_frontend() {
    print_info "Configuring Nginx for Frontend..."

    cat > /etc/nginx/sites-available/navtrack-frontend <<EOF
server {
    listen 80;
    listen [::]:80;
    server_name ${DOMAIN_FRONTEND};

    location /.well-known/acme-challenge/ {
        root /var/www/html;
    }

    # Proxy to application (Certbot will add HTTPS redirect later)
    location / {
        proxy_pass http://127.0.0.1:3000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
    }
}
EOF

    ln -sf /etc/nginx/sites-available/navtrack-frontend /etc/nginx/sites-enabled/
    print_success "Nginx configuration created for Frontend"
}

configure_nginx_api() {
    print_info "Configuring Nginx for Backend API..."

    cat > /etc/nginx/sites-available/navtrack-api <<EOF
server {
    listen 80;
    listen [::]:80;
    server_name ${DOMAIN_API};

    location /.well-known/acme-challenge/ {
        root /var/www/html;
    }

    client_max_body_size 10M;

    # Proxy to application (Certbot will add HTTPS redirect later)
    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
        proxy_read_timeout 300s;
        proxy_connect_timeout 75s;
    }
}
EOF

    ln -sf /etc/nginx/sites-available/navtrack-api /etc/nginx/sites-enabled/
    print_success "Nginx configuration created for Backend API"
}

configure_nginx_odoo_api() {
    print_info "Configuring Nginx for Odoo API..."

    cat > /etc/nginx/sites-available/navtrack-odoo-api <<EOF
server {
    listen 80;
    listen [::]:80;
    server_name ${DOMAIN_ODOO_API};

    location /.well-known/acme-challenge/ {
        root /var/www/html;
    }

    client_max_body_size 10M;

    # Proxy to application (Certbot will add HTTPS redirect later)
    location / {
        proxy_pass http://127.0.0.1:8081;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
        proxy_read_timeout 300s;
        proxy_connect_timeout 75s;
    }
}
EOF

    ln -sf /etc/nginx/sites-available/navtrack-odoo-api /etc/nginx/sites-enabled/
    print_success "Nginx configuration created for Odoo API"
}

configure_nginx_listener() {
    print_info "Configuring Nginx for GPS Listener (passthrough)..."

    cat > /etc/nginx/sites-available/navtrack-listener <<EOF
# GPS Listener - Direct TCP passthrough (no SSL for GPS devices)
# GPS devices connect directly to ports ${LISTENER_PORT_START}-${LISTENER_PORT_END}

server {
    listen 80;
    listen [::]:80;
    server_name ${DOMAIN_LISTENER};

    location /.well-known/acme-challenge/ {
        root /var/www/html;
    }

    # Info page (Certbot will add HTTPS redirect later)
    location / {
        return 200 'NavTrack GPS Listener Service\nGPS devices should connect directly to ports ${LISTENER_PORT_START}-${LISTENER_PORT_END}\n';
        add_header Content-Type text/plain;
    }
}
EOF

    ln -sf /etc/nginx/sites-available/navtrack-listener /etc/nginx/sites-enabled/
    print_success "Nginx configuration created for GPS Listener"
}

configure_firewall() {
    print_info "Configuring firewall rules..."

    # Check if UFW is available
    if command -v ufw &> /dev/null; then
        # Allow SSH
        ufw allow 22/tcp

        # Allow HTTP and HTTPS
        ufw allow 80/tcp
        ufw allow 443/tcp

        # Allow GPS Listener ports
        ufw allow ${LISTENER_PORT_START}:${LISTENER_PORT_END}/tcp

        # Enable UFW if not already enabled
        ufw --force enable

        print_success "Firewall configured (UFW)"
    else
        print_info "UFW not found, skipping firewall configuration. Please configure manually:"
        print_info "  - Allow ports 22, 80, 443"
        print_info "  - Allow ports ${LISTENER_PORT_START}-${LISTENER_PORT_END} for GPS devices"
    fi
}

obtain_ssl_certificates() {
    print_info "Obtaining SSL certificates from Let's Encrypt..."

    # Test Nginx configuration first
    nginx -t

    # Reload Nginx to apply configurations
    systemctl reload nginx

    # Array of domains
    domains=("$DOMAIN_FRONTEND" "$DOMAIN_API" "$DOMAIN_ODOO_API" "$DOMAIN_LISTENER")

    for domain in "${domains[@]}"; do
        print_info "Obtaining certificate for $domain..."

        # Check if certificate already exists
        if [ -d "/etc/letsencrypt/live/$domain" ]; then
            print_info "Certificate already exists for $domain, skipping..."
            continue
        fi

        certbot --nginx \
            -d "$domain" \
            --non-interactive \
            --agree-tos \
            --email "$EMAIL" \
            --redirect

        if [ $? -eq 0 ]; then
            print_success "SSL certificate obtained for $domain"
        else
            print_error "Failed to obtain SSL certificate for $domain"
            print_info "You may need to obtain it manually later with: certbot --nginx -d $domain"
        fi
    done

    # Set up auto-renewal
    systemctl enable certbot.timer
    systemctl start certbot.timer

    print_success "SSL certificates configured and auto-renewal enabled"
}

copy_project_files() {
    print_info "Copying project files to $INSTALL_DIR..."

    # Get the directory where this script is located
    SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

    # Create installation directory
    mkdir -p "$INSTALL_DIR"

    # Copy Directory.Build.props (CRITICAL - defines TargetFramework for .NET projects)
    print_info "Copying Directory.Build.props..."
    if [ -f "$SCRIPT_DIR/Directory.Build.props" ]; then
        cp "$SCRIPT_DIR/Directory.Build.props" "$INSTALL_DIR/" || print_error "Failed to copy Directory.Build.props"
    else
        print_error "Directory.Build.props not found in $SCRIPT_DIR - .NET builds will fail!"
    fi

    # Copy necessary files
    print_info "Copying backend files..."
    cp -r "$SCRIPT_DIR/backend" "$INSTALL_DIR/" 2>/dev/null || print_error "Backend directory not found in $SCRIPT_DIR"

    print_info "Copying frontend files..."
    cp -r "$SCRIPT_DIR/frontend" "$INSTALL_DIR/" 2>/dev/null || print_error "Frontend directory not found in $SCRIPT_DIR"

    print_info "Copying Odoo API files..."
    cp -r "$SCRIPT_DIR/Odoo.Navtrac.Api" "$INSTALL_DIR/" 2>/dev/null || print_error "Odoo.Navtrac.Api directory not found in $SCRIPT_DIR"

    # Copy any additional files needed
    if [ -f "$SCRIPT_DIR/run_web.sh" ]; then
        cp "$SCRIPT_DIR/run_web.sh" "$INSTALL_DIR/"
    fi

    print_success "Project files copied to $INSTALL_DIR"
}

build_and_start_services() {
    print_info "Building and starting Docker containers..."

    cd "$INSTALL_DIR"

    # Build images
    print_info "Building Docker images (this may take several minutes)..."
    docker compose -f docker-compose.prod.yml build

    # Start services
    print_info "Starting services..."
    docker compose -f docker-compose.prod.yml up -d

    print_success "All services started successfully"
}

create_systemd_service() {
    print_info "Creating systemd service for auto-start..."

    cat > /etc/systemd/system/navtrack.service <<EOF
[Unit]
Description=NavTrack GPS Tracking System
Requires=docker.service
After=docker.service

[Service]
Type=oneshot
RemainAfterExit=yes
WorkingDirectory=${INSTALL_DIR}
ExecStart=/usr/bin/docker compose -f docker-compose.prod.yml up -d
ExecStop=/usr/bin/docker compose -f docker-compose.prod.yml down
TimeoutStartSec=0

[Install]
WantedBy=multi-user.target
EOF

    systemctl daemon-reload
    systemctl enable navtrack.service

    print_success "Systemd service created and enabled"
}

display_summary() {
    echo ""
    echo "=============================================================================="
    print_success "NavTrack Installation Complete!"
    echo "=============================================================================="
    echo ""
    echo "Services are accessible at:"
    echo "  Frontend:    https://${DOMAIN_FRONTEND}"
    echo "  Backend API: https://${DOMAIN_API}"
    echo "  Odoo API:    https://${DOMAIN_ODOO_API}"
    echo "  GPS Devices: ${DOMAIN_LISTENER} (ports ${LISTENER_PORT_START}-${LISTENER_PORT_END})"
    echo ""
    echo "Useful commands:"
    echo "  View logs:           cd $INSTALL_DIR && docker compose -f docker-compose.prod.yml logs -f"
    echo "  Stop services:       cd $INSTALL_DIR && docker compose -f docker-compose.prod.yml down"
    echo "  Start services:      cd $INSTALL_DIR && docker compose -f docker-compose.prod.yml up -d"
    echo "  Restart services:    cd $INSTALL_DIR && docker compose -f docker-compose.prod.yml restart"
    echo "  View service status: systemctl status navtrack"
    echo ""
    echo "SSL Certificate renewal:"
    echo "  Auto-renewal is enabled via certbot timer"
    echo "  Manual renewal:      certbot renew"
    echo "  Test renewal:        certbot renew --dry-run"
    echo ""
    print_info "Note: Make sure your DNS records point to this server's IP address:"
    for domain in "$DOMAIN_FRONTEND" "$DOMAIN_API" "$DOMAIN_ODOO_API" "$DOMAIN_LISTENER"; do
        echo "  - $domain"
    done
    echo ""
    echo "=============================================================================="
}

###############################################################################
# Main Installation Flow
###############################################################################

main() {
    echo "=============================================================================="
    echo "                    NavTrack Production Installation                        "
    echo "=============================================================================="
    echo ""

    # Pre-flight checks
    check_root

    print_info "Starting installation process..."
    echo ""

    # Step 1: Install dependencies
    print_info "Step 1: Installing dependencies..."
    install_docker
    install_nginx
    install_certbot
    echo ""

    # Step 2: Copy project files
    print_info "Step 2: Preparing project files..."
    copy_project_files
    echo ""

    # Step 3: Create Docker Compose configuration
    print_info "Step 3: Creating Docker Compose configuration..."
    create_docker_compose
    echo ""

    # Step 4: Configure Nginx
    print_info "Step 4: Configuring Nginx reverse proxy..."
    configure_nginx_frontend
    configure_nginx_api
    configure_nginx_odoo_api
    configure_nginx_listener
    echo ""

    # Step 5: Configure firewall
    print_info "Step 5: Configuring firewall..."
    configure_firewall
    echo ""

    # Step 6: Build and start services
    print_info "Step 6: Building and starting services..."
    build_and_start_services
    echo ""

    # Step 7: Obtain SSL certificates
    print_info "Step 7: Obtaining SSL certificates..."
    print_info "Make sure DNS records are configured before proceeding!"
    read -p "Press Enter to continue with SSL certificate generation (or Ctrl+C to cancel)..."
    obtain_ssl_certificates
    echo ""

    # Step 8: Create systemd service
    print_info "Step 8: Setting up auto-start service..."
    create_systemd_service
    echo ""

    # Display summary
    display_summary
}

# Run main installation
main "$@"
