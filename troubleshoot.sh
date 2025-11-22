#!/bin/bash

###############################################################################
# NavTrack Troubleshooting Script
# Script para diagnosticar y resolver problemas comunes
###############################################################################

INSTALL_DIR="/opt/navtrack"
COMPOSE_FILE="docker-compose.prod.yml"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

print_section() {
    echo ""
    echo -e "${BLUE}========================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}========================================${NC}"
}

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

print_info() {
    echo -e "${BLUE}ℹ $1${NC}"
}

check_prerequisites() {
    print_section "Verificando Pre-requisitos"

    # Check if running as root
    if [ "$EUID" -ne 0 ]; then
        print_warning "Este script requiere privilegios de root para algunas verificaciones"
        print_info "Ejecute con sudo para obtener resultados completos"
    fi

    # Check installation directory
    if [ ! -d "$INSTALL_DIR" ]; then
        print_error "NavTrack no está instalado en $INSTALL_DIR"
        exit 1
    fi
    print_success "Directorio de instalación encontrado"

    # Check Docker
    if command -v docker &> /dev/null; then
        print_success "Docker instalado: $(docker --version)"
    else
        print_error "Docker no está instalado"
        return 1
    fi

    # Check Docker Compose
    if docker compose version &> /dev/null; then
        print_success "Docker Compose instalado: $(docker compose version)"
    else
        print_error "Docker Compose no está instalado"
        return 1
    fi

    # Check Nginx
    if command -v nginx &> /dev/null; then
        print_success "Nginx instalado: $(nginx -v 2>&1)"
    else
        print_error "Nginx no está instalado"
        return 1
    fi

    return 0
}

check_dns() {
    print_section "Verificando Configuración DNS"

    local domains=("gps-qa.inversionespereztaveras.com" "gps-api-qa.inversionespereztaveras.com" "gps-odoo-qa.inversionespereztaveras.com" "gps-listener-qa.inversionespereztaveras.com")

    for domain in "${domains[@]}"; do
        if host "$domain" > /dev/null 2>&1; then
            local ip=$(host "$domain" | grep "has address" | awk '{print $4}' | head -1)
            print_success "$domain → $ip"
        else
            print_error "No se pudo resolver $domain"
        fi
    done
}

check_ports() {
    print_section "Verificando Puertos"

    local ports=(80 443 27017 3000 8080 8081 7002)

    for port in "${ports[@]}"; do
        if netstat -tuln | grep -q ":$port "; then
            print_success "Puerto $port está escuchando"
        else
            print_warning "Puerto $port no está escuchando"
        fi
    done

    # Check GPS listener port range
    local listener_count=$(netstat -tuln | grep -c ":700[0-9]")
    if [ "$listener_count" -gt 0 ]; then
        print_success "$listener_count puertos de GPS listener están escuchando"
    else
        print_error "No hay puertos de GPS listener escuchando"
    fi
}

check_firewall() {
    print_section "Verificando Firewall"

    if command -v ufw &> /dev/null; then
        if ufw status | grep -q "Status: active"; then
            print_info "UFW está activo"
            echo ""
            ufw status | grep -E "(80|443|22|700)"
        else
            print_warning "UFW está instalado pero no activo"
        fi
    else
        print_info "UFW no está instalado"
    fi
}

check_containers() {
    print_section "Verificando Contenedores Docker"

    cd "$INSTALL_DIR" || exit 1

    local expected_containers=("frontend" "api" "odoo-api" "listener" "database")

    for container in "${expected_containers[@]}"; do
        local status=$(docker compose -f "$COMPOSE_FILE" ps "$container" --format json 2>/dev/null | jq -r '.[0].State' 2>/dev/null)

        if [ "$status" = "running" ]; then
            print_success "$container: Running"
        elif [ -z "$status" ] || [ "$status" = "null" ]; then
            print_error "$container: No existe"
        else
            print_error "$container: $status"

            # Show last 10 log lines for failed container
            echo "  Últimos logs:"
            docker compose -f "$COMPOSE_FILE" logs --tail=10 "$container" 2>&1 | sed 's/^/    /'
        fi
    done
}

check_nginx_config() {
    print_section "Verificando Configuración de Nginx"

    if nginx -t 2>&1 | grep -q "successful"; then
        print_success "Configuración de Nginx es válida"
    else
        print_error "Configuración de Nginx tiene errores:"
        nginx -t 2>&1 | sed 's/^/  /'
    fi

    # Check if sites are enabled
    local sites=("navtrack-frontend" "navtrack-api" "navtrack-odoo-api" "navtrack-listener")

    for site in "${sites[@]}"; do
        if [ -L "/etc/nginx/sites-enabled/$site" ]; then
            print_success "Sitio $site está habilitado"
        else
            print_error "Sitio $site NO está habilitado"
        fi
    done

    # Check Nginx status
    if systemctl is-active --quiet nginx; then
        print_success "Nginx está corriendo"
    else
        print_error "Nginx NO está corriendo"
        systemctl status nginx --no-pager | tail -10
    fi
}

check_ssl_certificates() {
    print_section "Verificando Certificados SSL"

    local domains=("gps-qa.inversionespereztaveras.com" "gps-api-qa.inversionespereztaveras.com" "gps-odoo-qa.inversionespereztaveras.com" "gps-listener-qa.inversionespereztaveras.com")

    for domain in "${domains[@]}"; do
        if [ -d "/etc/letsencrypt/live/$domain" ]; then
            local cert_path="/etc/letsencrypt/live/$domain/cert.pem"
            local expiry=$(openssl x509 -enddate -noout -in "$cert_path" 2>/dev/null | cut -d= -f2)

            if [ -n "$expiry" ]; then
                local expiry_epoch=$(date -d "$expiry" +%s 2>/dev/null)
                local now_epoch=$(date +%s)
                local days_left=$(( ($expiry_epoch - $now_epoch) / 86400 ))

                if [ "$days_left" -gt 30 ]; then
                    print_success "$domain: Válido por $days_left días"
                elif [ "$days_left" -gt 7 ]; then
                    print_warning "$domain: Expira en $days_left días"
                else
                    print_error "$domain: EXPIRA PRONTO ($days_left días)"
                fi
            fi
        else
            print_error "$domain: No tiene certificado SSL"
        fi
    done
}

check_database() {
    print_section "Verificando Base de Datos MongoDB"

    cd "$INSTALL_DIR" || exit 1

    # Check if container is running
    if docker compose -f "$COMPOSE_FILE" ps database | grep -q "running"; then
        print_success "Contenedor MongoDB está corriendo"

        # Check MongoDB connection
        if docker compose -f "$COMPOSE_FILE" exec -T database mongosh --quiet --eval "db.adminCommand('ping')" navtrack > /dev/null 2>&1; then
            print_success "Conexión a MongoDB exitosa"

            # Get database stats
            local db_size=$(docker compose -f "$COMPOSE_FILE" exec -T database mongosh --quiet --eval "db.stats().dataSize" navtrack 2>/dev/null)
            if [ -n "$db_size" ]; then
                print_info "Tamaño de base de datos: $db_size bytes"
            fi

            # Count collections
            local collections=$(docker compose -f "$COMPOSE_FILE" exec -T database mongosh --quiet --eval "db.getCollectionNames().length" navtrack 2>/dev/null)
            if [ -n "$collections" ]; then
                print_info "Número de colecciones: $collections"
            fi
        else
            print_error "No se puede conectar a MongoDB"
        fi
    else
        print_error "Contenedor MongoDB NO está corriendo"
    fi
}

check_api_endpoints() {
    print_section "Verificando Endpoints de API"

    # Check internal endpoints
    local endpoints=(
        "http://127.0.0.1:3000|Frontend"
        "http://127.0.0.1:8080|Backend API"
        "http://127.0.0.1:8081|Odoo API"
    )

    for endpoint in "${endpoints[@]}"; do
        local url=$(echo "$endpoint" | cut -d'|' -f1)
        local name=$(echo "$endpoint" | cut -d'|' -f2)

        local response=$(curl -s -o /dev/null -w "%{http_code}" --max-time 5 "$url" 2>/dev/null)

        if [ "$response" = "200" ] || [ "$response" = "301" ] || [ "$response" = "302" ]; then
            print_success "$name: HTTP $response"
        elif [ "$response" = "000" ]; then
            print_error "$name: No responde"
        else
            print_warning "$name: HTTP $response"
        fi
    done
}

check_disk_space() {
    print_section "Verificando Espacio en Disco"

    local disk_usage=$(df -h / | tail -1)
    echo "$disk_usage"

    local usage_percent=$(echo "$disk_usage" | awk '{print $5}' | sed 's/%//')

    if [ "$usage_percent" -lt 70 ]; then
        print_success "Espacio en disco suficiente (${usage_percent}% usado)"
    elif [ "$usage_percent" -lt 85 ]; then
        print_warning "Espacio en disco limitado (${usage_percent}% usado)"
    else
        print_error "Espacio en disco crítico (${usage_percent}% usado)"
    fi

    # Check Docker volumes
    print_info "Espacio usado por Docker:"
    docker system df
}

check_memory() {
    print_section "Verificando Uso de Memoria"

    free -h

    local mem_percent=$(free | grep Mem | awk '{print int($3/$2 * 100)}')

    if [ "$mem_percent" -lt 70 ]; then
        print_success "Uso de memoria normal (${mem_percent}%)"
    elif [ "$mem_percent" -lt 85 ]; then
        print_warning "Uso de memoria elevado (${mem_percent}%)"
    else
        print_error "Uso de memoria crítico (${mem_percent}%)"
    fi
}

check_logs_for_errors() {
    print_section "Buscando Errores en Logs Recientes"

    cd "$INSTALL_DIR" || exit 1

    local services=("frontend" "api" "odoo-api" "listener" "database")

    for service in "${services[@]}"; do
        local errors=$(docker compose -f "$COMPOSE_FILE" logs --tail=100 "$service" 2>&1 | grep -iE "error|exception|fatal" | wc -l)

        if [ "$errors" -eq 0 ]; then
            print_success "$service: Sin errores recientes"
        elif [ "$errors" -lt 5 ]; then
            print_warning "$service: $errors errores encontrados"
            echo "  Últimos errores:"
            docker compose -f "$COMPOSE_FILE" logs --tail=100 "$service" 2>&1 | grep -iE "error|exception|fatal" | tail -3 | sed 's/^/    /'
        else
            print_error "$service: $errors errores encontrados"
            echo "  Últimos errores:"
            docker compose -f "$COMPOSE_FILE" logs --tail=100 "$service" 2>&1 | grep -iE "error|exception|fatal" | tail -5 | sed 's/^/    /'
        fi
    done
}

suggest_fixes() {
    print_section "Sugerencias de Solución"

    echo "Si encuentra problemas, intente estas soluciones:"
    echo ""
    echo "1. Reiniciar servicios:"
    echo "   cd $INSTALL_DIR && docker compose -f $COMPOSE_FILE restart"
    echo ""
    echo "2. Verificar logs detallados:"
    echo "   cd $INSTALL_DIR && docker compose -f $COMPOSE_FILE logs -f [servicio]"
    echo ""
    echo "3. Reconstruir imágenes:"
    echo "   cd $INSTALL_DIR && docker compose -f $COMPOSE_FILE build && docker compose -f $COMPOSE_FILE up -d"
    echo ""
    echo "4. Verificar configuración de Nginx:"
    echo "   nginx -t"
    echo "   systemctl restart nginx"
    echo ""
    echo "5. Renovar certificados SSL:"
    echo "   certbot renew"
    echo ""
    echo "6. Limpiar recursos Docker:"
    echo "   docker system prune -a"
    echo ""
    echo "7. Verificar firewall:"
    echo "   ufw status"
    echo ""
}

run_full_diagnostic() {
    echo ""
    echo "╔══════════════════════════════════════════════════════════════╗"
    echo "║        NavTrack Troubleshooting Diagnostic Tool              ║"
    echo "╚══════════════════════════════════════════════════════════════╝"
    echo ""

    check_prerequisites || exit 1
    check_dns
    check_ports
    check_firewall
    check_containers
    check_nginx_config
    check_ssl_certificates
    check_database
    check_api_endpoints
    check_disk_space
    check_memory
    check_logs_for_errors
    suggest_fixes

    print_section "Diagnóstico Completo"
    print_success "Troubleshooting completado"
    echo ""
}

show_help() {
    cat << EOF
NavTrack Troubleshooting Script

Uso: $(basename "$0") [opción]

Opciones:
  full          Ejecutar diagnóstico completo (default)
  dns           Verificar solo DNS
  ports         Verificar solo puertos
  containers    Verificar solo contenedores
  nginx         Verificar solo Nginx
  ssl           Verificar solo certificados SSL
  database      Verificar solo base de datos
  api           Verificar solo endpoints de API
  logs          Verificar solo logs por errores
  help          Mostrar esta ayuda

Ejemplos:
  $(basename "$0")           # Diagnóstico completo
  $(basename "$0") full      # Diagnóstico completo
  $(basename "$0") logs      # Solo verificar logs
  $(basename "$0") database  # Solo verificar MongoDB

EOF
}

###############################################################################
# Main
###############################################################################

case "$1" in
    dns)
        check_prerequisites
        check_dns
        ;;
    ports)
        check_prerequisites
        check_ports
        ;;
    containers)
        check_prerequisites
        check_containers
        ;;
    nginx)
        check_prerequisites
        check_nginx_config
        ;;
    ssl)
        check_prerequisites
        check_ssl_certificates
        ;;
    database)
        check_prerequisites
        check_database
        ;;
    api)
        check_prerequisites
        check_api_endpoints
        ;;
    logs)
        check_prerequisites
        check_logs_for_errors
        ;;
    full|"")
        run_full_diagnostic
        ;;
    help|--help|-h)
        show_help
        ;;
    *)
        echo "Opción desconocida: $1"
        echo ""
        show_help
        exit 1
        ;;
esac
