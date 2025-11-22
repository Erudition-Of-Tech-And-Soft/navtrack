#!/bin/bash

###############################################################################
# NavTrack Management Script
# Herramienta para gestionar el sistema NavTrack en producción
###############################################################################

INSTALL_DIR="/opt/navtrack"
COMPOSE_FILE="docker-compose.prod.yml"

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

print_info() {
    echo -e "${BLUE}ℹ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

check_install_dir() {
    if [ ! -d "$INSTALL_DIR" ]; then
        print_error "NavTrack no está instalado en $INSTALL_DIR"
        exit 1
    fi
    cd "$INSTALL_DIR" || exit 1
}

show_help() {
    cat << EOF
NavTrack Management Tool

Uso: $(basename "$0") [comando]

Comandos disponibles:

  Gestión de Servicios:
    start           Iniciar todos los servicios
    stop            Detener todos los servicios
    restart         Reiniciar todos los servicios
    status          Ver estado de los servicios
    logs [servicio] Ver logs (opcional: frontend, api, odoo-api, listener, database)

  Actualización:
    update          Actualizar NavTrack a la última versión
    rebuild         Reconstruir imágenes Docker

  Base de Datos:
    backup          Crear backup de MongoDB
    restore [file]  Restaurar backup de MongoDB
    db-shell        Acceder al shell de MongoDB

  Monitoreo:
    stats           Ver estadísticas de uso de recursos
    health          Verificar salud de los servicios

  Mantenimiento:
    clean           Limpiar imágenes y contenedores no usados
    ssl-renew       Renovar certificados SSL manualmente

  Información:
    info            Mostrar información del sistema
    help            Mostrar esta ayuda

Ejemplos:
  $(basename "$0") start
  $(basename "$0") logs listener
  $(basename "$0") backup
  $(basename "$0") health

EOF
}

cmd_start() {
    check_install_dir
    print_info "Iniciando servicios NavTrack..."
    docker compose -f "$COMPOSE_FILE" up -d
    print_success "Servicios iniciados"
}

cmd_stop() {
    check_install_dir
    print_info "Deteniendo servicios NavTrack..."
    docker compose -f "$COMPOSE_FILE" down
    print_success "Servicios detenidos"
}

cmd_restart() {
    check_install_dir
    print_info "Reiniciando servicios NavTrack..."
    docker compose -f "$COMPOSE_FILE" restart
    print_success "Servicios reiniciados"
}

cmd_status() {
    check_install_dir
    print_info "Estado de servicios NavTrack:"
    echo ""
    docker compose -f "$COMPOSE_FILE" ps
    echo ""
    print_info "Estado del servicio systemd:"
    systemctl status navtrack --no-pager || true
}

cmd_logs() {
    check_install_dir
    local service=$1

    if [ -z "$service" ]; then
        print_info "Mostrando logs de todos los servicios (Ctrl+C para salir)..."
        docker compose -f "$COMPOSE_FILE" logs -f
    else
        print_info "Mostrando logs de $service (Ctrl+C para salir)..."
        docker compose -f "$COMPOSE_FILE" logs -f "$service"
    fi
}

cmd_update() {
    check_install_dir
    print_info "Actualizando NavTrack..."

    # Verificar si hay cambios en el repositorio
    if [ -d ".git" ]; then
        print_info "Descargando últimos cambios..."
        git pull

        print_info "Reconstruyendo imágenes..."
        docker compose -f "$COMPOSE_FILE" build

        print_info "Reiniciando servicios..."
        docker compose -f "$COMPOSE_FILE" up -d

        print_success "NavTrack actualizado exitosamente"
    else
        print_warning "No es un repositorio git. Por favor actualice manualmente."
    fi
}

cmd_rebuild() {
    check_install_dir
    print_info "Reconstruyendo imágenes Docker..."
    docker compose -f "$COMPOSE_FILE" build --no-cache
    print_success "Imágenes reconstruidas"

    read -p "¿Desea reiniciar los servicios con las nuevas imágenes? (y/N) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        docker compose -f "$COMPOSE_FILE" up -d
        print_success "Servicios reiniciados"
    fi
}

cmd_backup() {
    check_install_dir
    local backup_dir="$INSTALL_DIR/backups"
    local date=$(date +%Y%m%d_%H%M%S)
    local backup_file="navtrack-$date.archive.gz"

    mkdir -p "$backup_dir"

    print_info "Creando backup de MongoDB..."
    docker compose -f "$COMPOSE_FILE" exec -T database mongodump --archive --db navtrack | gzip > "$backup_dir/$backup_file"

    if [ $? -eq 0 ]; then
        print_success "Backup creado: $backup_dir/$backup_file"

        # Mostrar tamaño del backup
        local size=$(du -h "$backup_dir/$backup_file" | cut -f1)
        print_info "Tamaño del backup: $size"

        # Limpiar backups antiguos (más de 30 días)
        find "$backup_dir" -name "navtrack-*.archive.gz" -mtime +30 -delete
    else
        print_error "Error al crear backup"
        exit 1
    fi
}

cmd_restore() {
    check_install_dir
    local backup_file=$1

    if [ -z "$backup_file" ]; then
        print_error "Debe especificar el archivo de backup"
        echo "Uso: $(basename "$0") restore /path/to/backup.archive.gz"

        # Listar backups disponibles
        if [ -d "$INSTALL_DIR/backups" ]; then
            echo ""
            print_info "Backups disponibles:"
            ls -lh "$INSTALL_DIR/backups"/*.archive.gz 2>/dev/null || echo "  No hay backups disponibles"
        fi
        exit 1
    fi

    if [ ! -f "$backup_file" ]; then
        print_error "El archivo de backup no existe: $backup_file"
        exit 1
    fi

    print_warning "ADVERTENCIA: Esto sobrescribirá la base de datos actual."
    read -p "¿Está seguro que desea continuar? (y/N) " -n 1 -r
    echo

    if [[ $REPLY =~ ^[Yy]$ ]]; then
        print_info "Restaurando backup..."
        gunzip < "$backup_file" | docker compose -f "$COMPOSE_FILE" exec -T database mongorestore --archive --db navtrack --drop

        if [ $? -eq 0 ]; then
            print_success "Backup restaurado exitosamente"
        else
            print_error "Error al restaurar backup"
            exit 1
        fi
    else
        print_info "Restauración cancelada"
    fi
}

cmd_db_shell() {
    check_install_dir
    print_info "Conectando a MongoDB shell..."
    docker compose -f "$COMPOSE_FILE" exec database mongosh navtrack
}

cmd_stats() {
    check_install_dir
    print_info "Estadísticas de uso de recursos:"
    echo ""
    docker stats --no-stream
}

cmd_health() {
    check_install_dir
    print_info "Verificando salud de los servicios..."
    echo ""

    # Verificar contenedores
    local containers=$(docker compose -f "$COMPOSE_FILE" ps --services)
    local all_healthy=true

    for container in $containers; do
        local status=$(docker compose -f "$COMPOSE_FILE" ps "$container" --format json | jq -r '.[0].State' 2>/dev/null || echo "unknown")

        if [ "$status" = "running" ]; then
            print_success "$container: Running"
        else
            print_error "$container: $status"
            all_healthy=false
        fi
    done

    echo ""

    # Verificar conexiones HTTP
    print_info "Verificando endpoints HTTP..."

    check_endpoint() {
        local name=$1
        local url=$2
        local response=$(curl -s -o /dev/null -w "%{http_code}" --max-time 5 "$url" 2>/dev/null)

        if [ "$response" = "200" ] || [ "$response" = "301" ] || [ "$response" = "302" ]; then
            print_success "$name: HTTP $response"
        else
            print_warning "$name: HTTP $response (puede requerir autenticación)"
        fi
    }

    check_endpoint "Frontend" "http://127.0.0.1:3000"
    check_endpoint "API" "http://127.0.0.1:8080"
    check_endpoint "Odoo API" "http://127.0.0.1:8081"

    echo ""

    # Verificar espacio en disco
    print_info "Uso de disco:"
    df -h / | grep -v Filesystem

    # Verificar uso de memoria
    print_info "Uso de memoria:"
    free -h | grep -E "Mem|Swap"

    echo ""

    if [ "$all_healthy" = true ]; then
        print_success "Todos los servicios están saludables"
    else
        print_warning "Algunos servicios tienen problemas"
    fi
}

cmd_clean() {
    print_info "Limpiando recursos Docker no utilizados..."

    print_info "Eliminando contenedores detenidos..."
    docker container prune -f

    print_info "Eliminando imágenes sin usar..."
    docker image prune -a -f

    print_info "Eliminando volúmenes no utilizados..."
    docker volume prune -f

    print_info "Eliminando redes no utilizadas..."
    docker network prune -f

    print_success "Limpieza completada"

    echo ""
    print_info "Espacio liberado:"
    docker system df
}

cmd_ssl_renew() {
    print_info "Renovando certificados SSL..."
    certbot renew

    if [ $? -eq 0 ]; then
        print_success "Certificados renovados"
        print_info "Recargando Nginx..."
        systemctl reload nginx
        print_success "Nginx recargado"
    else
        print_error "Error al renovar certificados"
        exit 1
    fi
}

cmd_info() {
    check_install_dir

    cat << EOF

╔══════════════════════════════════════════════════════════════╗
║              NavTrack System Information                     ║
╚══════════════════════════════════════════════════════════════╝

EOF

    print_info "Instalación:"
    echo "  Directorio: $INSTALL_DIR"
    echo "  Compose file: $COMPOSE_FILE"
    echo ""

    print_info "URLs de Servicios:"
    echo "  Frontend:    https://gps-qa.inversionespereztaveras.com"
    echo "  API:         https://gps-api-qa.inversionespereztaveras.com"
    echo "  Odoo API:    https://gps-odoo-qa.inversionespereztaveras.com"
    echo "  GPS Listener: gps-listener-qa.inversionespereztaveras.com"
    echo ""

    print_info "Versiones:"
    echo -n "  Docker: "
    docker --version
    echo -n "  Docker Compose: "
    docker compose version
    echo -n "  Nginx: "
    nginx -v 2>&1 | cut -d/ -f2
    echo ""

    print_info "Estado de servicios:"
    docker compose -f "$COMPOSE_FILE" ps --format "table {{.Service}}\t{{.Status}}\t{{.Ports}}"
    echo ""

    print_info "Uso de recursos:"
    docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}"
    echo ""

    print_info "Certificados SSL:"
    certbot certificates 2>/dev/null | grep -E "Certificate Name:|Expiry Date:" || echo "  No se encontraron certificados"
    echo ""
}

###############################################################################
# Main
###############################################################################

main() {
    local command=$1
    shift

    case "$command" in
        start)
            cmd_start
            ;;
        stop)
            cmd_stop
            ;;
        restart)
            cmd_restart
            ;;
        status)
            cmd_status
            ;;
        logs)
            cmd_logs "$@"
            ;;
        update)
            cmd_update
            ;;
        rebuild)
            cmd_rebuild
            ;;
        backup)
            cmd_backup
            ;;
        restore)
            cmd_restore "$@"
            ;;
        db-shell)
            cmd_db_shell
            ;;
        stats)
            cmd_stats
            ;;
        health)
            cmd_health
            ;;
        clean)
            cmd_clean
            ;;
        ssl-renew)
            cmd_ssl_renew
            ;;
        info)
            cmd_info
            ;;
        help|--help|-h)
            show_help
            ;;
        *)
            if [ -z "$command" ]; then
                show_help
            else
                print_error "Comando desconocido: $command"
                echo ""
                show_help
                exit 1
            fi
            ;;
    esac
}

# Verificar si se ejecuta como root para comandos que lo requieren
if [ "$EUID" -ne 0 ] && [ "$1" != "help" ] && [ "$1" != "--help" ] && [ "$1" != "-h" ]; then
    print_warning "Algunos comandos requieren privilegios de root. Considere usar sudo."
fi

main "$@"
